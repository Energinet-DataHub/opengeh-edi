﻿// Copyright 2020 Energinet DataHub A/S
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

using System.Collections.Generic;
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

    public QueryInput BuildFrom(GetMessagesQuery request)
    {
        if (request.CreationPeriod is not null)
        {
            AddFilter(
                "CreatedAt BETWEEN @StartOfPeriod AND @EndOfPeriod",
                new KeyValuePair<string, object>("StartOfPeriod", request.CreationPeriod.DateToSearchFrom.ToString()),
                new KeyValuePair<string, object>("EndOfPeriod", request.CreationPeriod.DateToSearchTo.ToString()));
        }

        if (request.MessageId is not null)
        {
            AddFilter(
                request.IncludeRelatedMessages ? "(MessageId=@MessageId or RelatedToMessageId = @MessageId)" : "MessageId=@MessageId",
                new KeyValuePair<string, object>("MessageId", request.MessageId));
        }

        if (request.SenderNumber is not null && request.ReceiverNumber is not null)
        {
            AddFilter(
                "(SenderNumber=@SenderNumber OR ReceiverNumber=@ReceiverNumber)",
                new KeyValuePair<string, object>("SenderNumber", request.SenderNumber),
                new KeyValuePair<string, object>("ReceiverNumber", request.ReceiverNumber));
        }
        else if (request.SenderNumber is not null)
        {
            AddFilter(
                "SenderNumber=@SenderNumber",
                new KeyValuePair<string, object>("SenderNumber", request.SenderNumber));
        }
        else if (request.ReceiverNumber is not null)
        {
            AddFilter(
                "ReceiverNumber=@ReceiverNumber",
                new KeyValuePair<string, object>("ReceiverNumber", request.ReceiverNumber));
        }

        if (request.DocumentTypes is not null)
        {
            AddFilter(
                "DocumentType in @DocumentType",
                new KeyValuePair<string, object>("DocumentType", request.DocumentTypes));
        }

        if (request.BusinessReasons is not null)
        {
            AddFilter(
                "BusinessReason in @BusinessReason",
                new KeyValuePair<string, object>("BusinessReason", request.BusinessReasons));
        }

        if (_actorIdentity.HasRestriction(Restriction.Owned))
        {
            AddFilter(
                "(ReceiverNumber=@Requester OR SenderNumber=@Requester)",
                new KeyValuePair<string, object>("Requester", _actorIdentity.ActorNumber.Value));
        }
        else if (!_actorIdentity.HasRestriction(Restriction.None))
        {
            throw new InvalidRestrictionException($"Invalid restriction for fetching archived messages. Must be either {nameof(Restriction.Owned)} or {nameof(Restriction.None)}. ActorNumber: {_actorIdentity.ActorNumber.Value}; Restriction: {_actorIdentity.Restriction.Name}");
        }

        return new QueryInput(BuildStatement(request.IncludeRelatedMessages, request.MessageId), _queryParameters);
    }

    private string BuildStatement(bool includeRelatedMessages, string? messageId)
    {
        var whereClause = _statement.Count > 0 ? $" WHERE {string.Join(" AND ", _statement)}" : string.Empty;
        string sqlStatement;

        if (includeRelatedMessages == true && messageId is not null)
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
            sqlStatement = "SELECT DISTINCT t2.Id, t2.MessageId, t2.DocumentType, t2.SenderNumber, t2.ReceiverNumber, t2.CreatedAt, t2.BusinessReason " +
                           $"FROM ( SELECT * FROM dbo.ArchivedMessages {whereClause} ) AS t1 " +
                           "INNER JOIN dbo.ArchivedMessages as t2 " +
                           "ON t1.RelatedToMessageId = t2.RelatedToMessageId OR t1.RelatedToMessageId = t2.MessageId OR t1.MessageId= t2.MessageId";
        }
        else
        {
            var selectStatement = "SELECT Id, MessageId, DocumentType, SenderNumber, ReceiverNumber, CreatedAt, BusinessReason FROM dbo.ArchivedMessages";
            sqlStatement = selectStatement + whereClause;
        }

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
