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

using System.Collections.Generic;
using System.Linq;
using Dapper;

namespace Application.SearchMessages;

internal sealed class QueryBuilder
{
    private readonly DynamicParameters _queryParameters = new();
    private readonly List<string> _statement = new();

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
                "Id=@MessageId",
                new KeyValuePair<string, object>("MessageId", request.MessageId.Value.ToString()));
        }

        if (request.SenderNumber is not null)
        {
            AddFilter(
                "SenderNumber=@SenderNumber",
                new KeyValuePair<string, object>("SenderNumber", request.SenderNumber));
        }

        if (request.ReceiverNumber is not null)
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

        if (request.ProcessTypes is not null)
        {
            AddFilter(
                "ProcessType in @ProcessType",
                new KeyValuePair<string, object>("ProcessType", request.ProcessTypes));
        }

        return new QueryInput(BuildStatement(), _queryParameters);
    }

    private string BuildStatement()
    {
        var selectStatement = "SELECT Id AS MessageId, DocumentType, SenderNumber, ReceiverNumber, CreatedAt, ProcessType FROM dbo.ArchivedMessages";

        if (_statement.Count > 0)
        {
            selectStatement += " WHERE " + string.Join(" AND ", _statement);
        }

        return selectStatement;
    }

    private void AddFilter(string whereStatement, params KeyValuePair<string, object>[] queryParameters)
    {
        if (!queryParameters.Any())
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
