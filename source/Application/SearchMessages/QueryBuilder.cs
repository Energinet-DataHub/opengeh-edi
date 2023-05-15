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
using Dapper;

namespace Application.SearchMessages;

internal static class QueryBuilder
{
    public static QueryInput BuildFrom(GetMessagesQuery request)
    {
        var queryParameters = new DynamicParameters();
        var statement = new List<string>();

        if (request.CreationPeriod is not null)
        {
            statement.Add("CreatedAt BETWEEN @StartOfPeriod AND @EndOfPeriod");
            queryParameters.Add("StartOfPeriod", request.CreationPeriod.DateToSearchFrom.ToString());
            queryParameters.Add("EndOfPeriod", request.CreationPeriod.DateToSearchTo.ToString());
        }

        if (request.MessageId is not null)
        {
            statement.Add("Id=@MessageId");
            queryParameters.Add("MessageId", request.MessageId.Value.ToString());
        }

        if (request.SenderNumber is not null)
        {
            statement.Add("SenderNumber=@SenderNumber");
            queryParameters.Add("SenderNumber", request.SenderNumber);
        }

        if (request.ReceiverNumber is not null)
        {
            statement.Add("ReceiverNumber=@ReceiverNumber");
            queryParameters.Add("ReceiverNumber", request.ReceiverNumber);
        }

        return new QueryInput(BuildStatement(statement), queryParameters);
    }

    private static string BuildStatement(IReadOnlyCollection<string> statement)
    {
        var selectStatement = "SELECT Id AS MessageId, DocumentType, SenderNumber, ReceiverNumber, CreatedAt FROM dbo.ArchivedMessages";
        if (statement.Count > 0)
        {
            selectStatement += " WHERE " + string.Join(" AND ", statement);
        }

        return selectStatement;
    }
}
