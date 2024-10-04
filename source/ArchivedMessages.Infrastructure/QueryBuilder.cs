// Copyright 2020 Energinet DataHub A/S
//
// Licensed under the Apache License, Version 2.0 (the "License2");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Dapper;
using Energinet.DataHub.EDI.ArchivedMessages.Interfaces;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Exceptions;

namespace Energinet.DataHub.EDI.ArchivedMessages.Infrastructure;

internal sealed class QueryBuilder
{
    private readonly ActorIdentity _actorIdentity;
    private readonly DynamicParameters _queryParameters = new();
    private readonly List<string> _statement = new();

    public QueryBuilder(ActorIdentity actorIdentity)
    {
        _actorIdentity = actorIdentity;
    }

    /// <summary>
    /// Build a query for fetching archived messages based on the given query options.
    /// </summary>
    /// <param name="query"></param>
    public QueryInput BuildFrom(GetMessagesQuery query)
    {
        if (query.CreationPeriod is not null)
        {
            AddFilter(
                "CreatedAt BETWEEN @StartOfPeriod AND @EndOfPeriod",
                new KeyValuePair<string, object>("StartOfPeriod", query.CreationPeriod.DateToSearchFrom.ToString()),
                new KeyValuePair<string, object>("EndOfPeriod", query.CreationPeriod.DateToSearchTo.ToString()));
        }

        if (query.MessageId is not null)
        {
            AddFilter(
                query.IncludeRelatedMessages ? "(MessageId=@MessageId or RelatedToMessageId = @MessageId)" : "MessageId=@MessageId",
                new KeyValuePair<string, object>("MessageId", query.MessageId));
        }

        if (query.SenderNumber is not null && query.ReceiverNumber is not null)
        {
            AddFilter(
                "(SenderNumber=@SenderNumber OR ReceiverNumber=@ReceiverNumber)",
                new KeyValuePair<string, object>("SenderNumber", query.SenderNumber),
                new KeyValuePair<string, object>("ReceiverNumber", query.ReceiverNumber));
        }
        else if (query.SenderNumber is not null)
        {
            AddFilter(
                "SenderNumber=@SenderNumber",
                new KeyValuePair<string, object>("SenderNumber", query.SenderNumber));
        }
        else if (query.ReceiverNumber is not null)
        {
            AddFilter(
                "ReceiverNumber=@ReceiverNumber",
                new KeyValuePair<string, object>("ReceiverNumber", query.ReceiverNumber));
        }

        if (query.DocumentTypes is not null)
        {
            AddFilter(
                "DocumentType in @DocumentType",
                new KeyValuePair<string, object>("DocumentType", query.DocumentTypes));
        }

        if (query.BusinessReasons is not null)
        {
            AddFilter(
                "BusinessReason in @BusinessReason",
                new KeyValuePair<string, object>("BusinessReason", query.BusinessReasons));
        }

        if (_actorIdentity.HasRestriction(Restriction.Owned))
        {
            AddFilter(
                "(ReceiverNumber=@Requester OR SenderNumber=@Requester)",
                new KeyValuePair<string, object>("Requester", _actorIdentity.ActorNumber.Value));
            AddFilter(
                "(ReceiverRoleCode=@RequesterRoleCode OR SenderRoleCode=@RequesterRoleCode)",
                new KeyValuePair<string, object>("RequesterRoleCode", _actorIdentity.ActorRole.Code));
        }
        else if (!_actorIdentity.HasRestriction(Restriction.None))
        {
            throw new InvalidRestrictionException($"Invalid restriction for fetching archived messages. Must be either {nameof(Restriction.Owned)} or {nameof(Restriction.None)}. ActorNumber: {_actorIdentity.ActorNumber.Value}; Restriction: {_actorIdentity.Restriction.Name}");
        }

        return new QueryInput(BuildStatement(query), _queryParameters);
    }

    private static string WherePaginationPosition(FieldToSortBy fieldToSortBy, DirectionToSortBy directionToSortBy, SortingCursor cursor, bool isForward)
    {
        if (cursor.SortedFieldValue is null)
        {
            return isForward ? $" (RecordId < {cursor.RecordId} OR {cursor.RecordId} = 0) "
                    : $" (RecordId > {cursor.RecordId} OR {cursor.RecordId} = 0) ";
        }

        // Toggle the sort direction if navigating backwards, because sql use top to limit the result
        var sortingDirection = isForward ? directionToSortBy.Identifier == DirectionToSortBy.Descending.Identifier ? "<" : ">"
                : directionToSortBy.Identifier == DirectionToSortBy.Descending.Identifier ? ">" : "<";
        return isForward
            ? $"""
                  (({fieldToSortBy.Identifier} = '{cursor.SortedFieldValue}' AND (RecordId < {cursor.RecordId} OR {cursor.RecordId} = 0)) 
                  OR ({fieldToSortBy.Identifier} {sortingDirection} '{cursor.SortedFieldValue}'))
              """
            : $"""
                   (({fieldToSortBy.Identifier} = '{cursor.SortedFieldValue}' AND (RecordId > {cursor.RecordId} OR {cursor.RecordId} = 0)) 
                   OR ({fieldToSortBy.Identifier} {sortingDirection} '{cursor.SortedFieldValue}')) 
               """;
    }

    private string OrderBy(FieldToSortBy fieldToSortBy, DirectionToSortBy directionToSortBy, bool navigatingForward)
    {
        var pagingDirection = navigatingForward ? "DESC" : "ASC";
        // Toggle the sort direction if navigating backwards, because sql use top to limit the result
        var sortDirection = navigatingForward ? directionToSortBy : directionToSortBy.Identifier == DirectionToSortBy.Ascending.Identifier ? DirectionToSortBy.Descending : DirectionToSortBy.Ascending;
        return $" ORDER BY {fieldToSortBy.Identifier} {sortDirection.Identifier}, RecordId {pagingDirection}";
    }

    private string BuildStatement(GetMessagesQuery query)
    {
        var whereClause = " WHERE ";
        whereClause += _statement.Count > 0 ? $"{string.Join(" AND ", _statement)} AND " : string.Empty;
        whereClause += WherePaginationPosition(query.Pagination.SortBy, query.Pagination.DirectionToSortBy, query.Pagination.Cursor, query.Pagination.NavigationForward);
        string sqlStatement;

        if (query.IncludeRelatedMessages == true && query.MessageId is not null)
        {
            // Messages may be related in different ways, hence we have the following 3 cases:
            // 1. The message is related to other messages (Searching for a request with responses)
            // 2. The message is related to a message that is related to another message (Searching for a response with a request, where the request has multiple responses)
            // 3. The message is not related to any other messages
            // Case 1 and 2 may be solve by joining every request onto a response (t1.RelatedToMessageId = t2.MessageId)
            // and every response onto another response (t1.RelatedToMessageId = t2.RelatedToMessageId)
            // Hence, if we were in case 1: Table 2 would consist of all responses to the request and the request itself (containing duplications)
            // For case 2: Table 2 would consists of the response you searched for, all other responses which has a reference to the same request and the request itself (containing duplications)

            // Case 3 is solved by joining every message onto itself (t1.MessageId = t2.MessageId)
            // Since table 2 would be empty without it, hence we would not get anything when we do our inner join
            sqlStatement = $"SELECT DISTINCT TOP ({query.Pagination.PageSize}) t2.RecordId, t2.Id, t2.MessageId, t2.DocumentType, t2.SenderNumber, t2.ReceiverNumber, t2.CreatedAt, t2.BusinessReason " +
                           $"FROM ( SELECT * FROM dbo.ArchivedMessages {whereClause} ) AS t1 " +
                           "INNER JOIN dbo.ArchivedMessages as t2 " +
                           "ON t1.RelatedToMessageId = t2.RelatedToMessageId OR t1.RelatedToMessageId = t2.MessageId OR t1.MessageId= t2.MessageId";
        }
        else
        {
            var selectStatement = $"SELECT TOP ({query.Pagination.PageSize}) RecordId, Id, MessageId, DocumentType, SenderNumber, ReceiverNumber, CreatedAt, BusinessReason FROM dbo.ArchivedMessages";
            sqlStatement = selectStatement + whereClause;
        }

        sqlStatement += OrderBy(query.Pagination.SortBy, query.Pagination.DirectionToSortBy, query.Pagination.NavigationForward);
        return sqlStatement;
    }

    private void AddFilter(string whereStatement, params KeyValuePair<string, object>[] queryParameters)
    {
        if (queryParameters.Length == 0)
        {
            return;
        }

        _statement.Add(whereStatement);

        foreach (var queryParameter in queryParameters)
        {
            _queryParameters.Add(queryParameter.Key, queryParameter.Value);
        }
    }
}
