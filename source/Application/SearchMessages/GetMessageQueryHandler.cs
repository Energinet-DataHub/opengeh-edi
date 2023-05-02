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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Configuration.DataAccess;
using Dapper;
using Domain.ArchivedMessages;
using MediatR;

namespace Application.SearchMessages;

public class GetMessageQueryHandler : IRequestHandler<GetMessagesQuery, MessageSearchResult>
{
    private readonly IArchivedMessageRepository _archivedMessageRepository;
    private readonly IDatabaseConnectionFactory _connectionFactory;

    public GetMessageQueryHandler(IArchivedMessageRepository archivedMessageRepository, IDatabaseConnectionFactory connectionFactory)
    {
        _archivedMessageRepository = archivedMessageRepository;
        _connectionFactory = connectionFactory;
    }

    public async Task<MessageSearchResult> Handle(GetMessagesQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.CreationPeriod is not null)
        {
            using var connection = await _connectionFactory.GetConnectionAndOpenAsync().ConfigureAwait(false);
            var archivedMessages =
                await connection.QueryAsync<MessageInfo>(
                    "SELECT Id AS MessageId, DocumentType, SenderNumber, ReceiverNumber, CreatedAt FROM dbo.ArchivedMessages WHERE CreatedAt BETWEEN @StartOfPeriod AND @EndOfPeriod",
                    new
                    {
                        StartOfPeriod = request.CreationPeriod.DateToSearchFrom.ToString(),
                        EndOfPeriod = request.CreationPeriod.DateToSearchTo.ToString(),
                    })
                    .ConfigureAwait(false);
            return new MessageSearchResult(archivedMessages.ToList().AsReadOnly());
        }

        var messages = await _archivedMessageRepository.GetAllAsync().ConfigureAwait(false);
        return new MessageSearchResult(messages.Select(message => new MessageInfo(message.Id, message.DocumentType.Name, message.SenderNumber.Value, message.ReceiverNumber.Value, message.CreatedAt)).ToList().AsReadOnly());
    }
}
