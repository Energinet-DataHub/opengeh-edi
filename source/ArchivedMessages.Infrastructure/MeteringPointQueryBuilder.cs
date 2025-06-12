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
using Energinet.DataHub.EDI.ArchivedMessages.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Exceptions;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.ArchivedMessages.Infrastructure;

internal sealed class MeteringPointQueryBuilder(ActorIdentity actorIdentity)
{
    private readonly DynamicParameters _queryParameters = new();
    private readonly List<string> _statement = new();
    private readonly ActorIdentity _actorIdentity = actorIdentity;

    /// <summary>
    /// Build a query for fetching archived messages based on the given query options.
    /// </summary>
    /// <param name="query"></param>
    public QueryInput BuildFrom(GetMeteringPointMessagesQuery query)
    {
        if (query.MeteringPointId is null)
            throw new ArgumentNullException(nameof(query.MeteringPointId), "MeteringPointId cannot be null.");
        if (query.CreationPeriod is null)
            throw new ArgumentNullException(nameof(query.CreationPeriod), "CreationPeriod cannot be null.");

        AddFilter(
            "@MeteringPointId IN (SELECT [mpids].[value] FROM OPENJSON(MeteringPointIds) WITH ([value] NVARCHAR(50) '$') AS [mpids])",
            new KeyValuePair<string, object>("MeteringPointId", query.MeteringPointId.Value));

        AddFilter(
            "CreatedAt BETWEEN @StartOfPeriod AND @EndOfPeriod",
            new KeyValuePair<string, object>("StartOfPeriod", query.CreationPeriod.DateToSearchFrom.ToString()),
            new KeyValuePair<string, object>("EndOfPeriod", query.CreationPeriod.DateToSearchTo.ToString()));

        if (query.Sender is not null && query.Receiver is not null)
        {
            AddFilter(
                "(SenderNumber=@SenderNumber OR ReceiverNumber=@ReceiverNumber)",
                new KeyValuePair<string, object>("SenderNumber", query.Sender.ActorNumber.Value),
                new KeyValuePair<string, object>("ReceiverNumber", query.Receiver.ActorNumber.Value));

            AddFilter(
                "(SenderRole=@SenderRole OR ReceiverRole=@ReceiverRole)",
                new KeyValuePair<string, object>("SenderRole", query.Sender.ActorRole.DatabaseValue),
                new KeyValuePair<string, object>("ReceiverRole", query.Receiver.ActorRole.DatabaseValue));
        }
        else if (query.Sender is not null)
        {
            AddFilter(
                "SenderNumber=@SenderNumber",
                new KeyValuePair<string, object>("SenderNumber", query.Sender.ActorNumber.Value));
            AddFilter(
                "SenderRole=@SenderRole",
                new KeyValuePair<string, object>("SenderRole", query.Sender.ActorRole.DatabaseValue));
        }
        else if (query.Receiver is not null)
        {
            AddFilter(
                "ReceiverNumber=@ReceiverNumber",
                new KeyValuePair<string, object>("ReceiverNumber", query.Receiver.ActorNumber.Value));
            AddFilter(
                "ReceiverRole=@ReceiverRole",
                new KeyValuePair<string, object>("ReceiverRole", query.Receiver.ActorRole.DatabaseValue));
        }

        if (query.DocumentTypes is not null)
        {
            AddFilter(
                "DocumentType in @DocumentType",
                new KeyValuePair<string, object>("DocumentType", query.DocumentTypes
                    .Select(x => x.DatabaseValue)));
        }

        if (_actorIdentity.HasRestriction(Restriction.Owned))
        {
            AddFilter(
                "(ReceiverNumber=@Requester OR SenderNumber=@Requester)",
                new KeyValuePair<string, object>("Requester", _actorIdentity.ActorNumber.Value));
            AddFilter(
                "(ReceiverRole=@RequesterRole OR SenderRole=@RequesterRole)",
                new KeyValuePair<string, object>("RequesterRole", _actorIdentity.ActorRole.DatabaseValue));
        }
        else if (!_actorIdentity.HasRestriction(Restriction.None))
        {
            throw new InvalidRestrictionException($"Invalid restriction for fetching archived messages. Must be either {nameof(Restriction.Owned)} or {nameof(Restriction.None)}. ActorNumber: {_actorIdentity.ActorNumber.Value}; Restriction: {_actorIdentity.Restriction.Name}");
        }

        return new QueryInput(BuildStatement(query), BuildTotalCountStatement(query), _queryParameters);
    }

    private static string WherePaginationPosition(FieldToSortBy fieldToSortBy, DirectionToSortBy directionToSortBy, SortingCursor cursor, bool isForward)
    {
        if (cursor.SortedFieldValue is null)
        {
            return isForward ? $" (PaginationCursorValue < {cursor.CursorPosition} OR {cursor.CursorPosition} = 0) "
                    : $" (PaginationCursorValue > {cursor.CursorPosition} OR {cursor.CursorPosition} = 0) ";
        }

        // Toggle the sort direction if navigating backwards, because sql use top to limit the result
        var sortingDirection = isForward ? directionToSortBy.Identifier == DirectionToSortBy.Descending.Identifier ? "<" : ">"
                : directionToSortBy.Identifier == DirectionToSortBy.Descending.Identifier ? ">" : "<";
        return isForward
            ? $"""
                  (({fieldToSortBy.Identifier} = '{cursor.SortedFieldValue}' AND (PaginationCursorValue < {cursor.CursorPosition} OR {cursor.CursorPosition} = 0))
                  OR ({fieldToSortBy.Identifier} {sortingDirection} '{cursor.SortedFieldValue}'))
              """
            : $"""
                   (({fieldToSortBy.Identifier} = '{cursor.SortedFieldValue}' AND (PaginationCursorValue > {cursor.CursorPosition} OR {cursor.CursorPosition} = 0))
                   OR ({fieldToSortBy.Identifier} {sortingDirection} '{cursor.SortedFieldValue}'))
               """;
    }

    private string OrderBy(FieldToSortBy fieldToSortBy, DirectionToSortBy sortByDirection, bool navigatingForward)
    {
        // DocumentType is not sortable, so we need to sort by CreatedAt instead, since it uses TinyInt
        if (fieldToSortBy.Equals(FieldToSortBy.DocumentType))
            fieldToSortBy = FieldToSortBy.CreatedAt;

        var pagingDirection = navigatingForward ? "DESC" : "ASC";
        // Toggle the sort direction if navigating backwards, because sql use top to limit the result
        var sortDirection = navigatingForward ? sortByDirection : sortByDirection.Identifier == DirectionToSortBy.Ascending.Identifier ? DirectionToSortBy.Descending : DirectionToSortBy.Ascending;
        return $" ORDER BY {fieldToSortBy.Identifier} {sortDirection.Identifier}, PaginationCursorValue {pagingDirection}";
    }

    private string BuildStatement(GetMeteringPointMessagesQuery query)
    {
        var whereClause = " WHERE ";
        whereClause += _statement.Count > 0 ? $"{string.Join(" AND ", _statement)} AND " : string.Empty;
        whereClause += WherePaginationPosition(query.Pagination.FieldToSortBy, query.Pagination.DirectionToSortBy, query.Pagination.Cursor, query.Pagination.NavigationForward);

        var selectStatement = $"SELECT TOP ({query.Pagination.PageSize}) PaginationCursorValue, Id, MessageId, DocumentType, SenderNumber, SenderRole, ReceiverNumber, ReceiverRole, CreatedAt, BusinessReason FROM dbo.MeteringPointArchivedMessages";
        var sqlStatement = selectStatement + whereClause;

        sqlStatement += OrderBy(query.Pagination.FieldToSortBy, query.Pagination.DirectionToSortBy, query.Pagination.NavigationForward);
        return sqlStatement;
    }

    private string BuildTotalCountStatement(GetMeteringPointMessagesQuery query)
    {
        var whereClause = _statement.Count > 0 ? $" WHERE {string.Join(" AND ", _statement)} " : string.Empty;

        var selectStatement = $"SELECT Count(*) FROM dbo.MeteringPointArchivedMessages";
        var sqlStatement = selectStatement + whereClause;

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
