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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Configuration.DataAccess;
using Dapper;
using MediatR;

namespace Application.SearchMessages;

public class GetMessageQueryHandler : IRequestHandler<GetMessagesQuery, MessageSearchResult>
{
    private readonly IDatabaseConnectionFactory _connectionFactory;

    public GetMessageQueryHandler(IDatabaseConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<MessageSearchResult> Handle(GetMessagesQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var selectStatement =
            "SELECT Id AS MessageId, DocumentType, SenderNumber, ReceiverNumber, CreatedAt FROM dbo.ArchivedMessages";
        var queryParameters = new DynamicParameters();

        if (request.CreationPeriod is null && request.MessageId is not null)
        {
            selectStatement += " WHERE CreatedAt = CreatedAt";
        }

        if (request.CreationPeriod is not null)
        {
            selectStatement += " WHERE CreatedAt BETWEEN @StartOfPeriod AND @EndOfPeriod";
            queryParameters.Add("StartOfPeriod", request.CreationPeriod.DateToSearchFrom.ToString());
            queryParameters.Add("EndOfPeriod", request.CreationPeriod.DateToSearchTo.ToString());
        }

        if (request.MessageId is not null)
        {
            selectStatement += " AND Id = @MessageId";
            queryParameters.Add("MessageId", request.MessageId.Value.ToString());
        }

        using var connection = await _connectionFactory.GetConnectionAndOpenAsync().ConfigureAwait(false);
        var archivedMessages =
            await connection.QueryAsync<MessageInfo>(
                    selectStatement,
                    queryParameters)
                .ConfigureAwait(false);
        return new MessageSearchResult(archivedMessages.ToList().AsReadOnly());
    }
}
