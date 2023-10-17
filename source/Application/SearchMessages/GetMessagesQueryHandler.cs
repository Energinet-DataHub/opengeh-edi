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
using Dapper;
using Energinet.DataHub.EDI.Application.Configuration.DataAccess;
using MediatR;

namespace Energinet.DataHub.EDI.Application.SearchMessages;

public class GetMessagesQueryHandler : IRequestHandler<GetMessagesQuery, MessageSearchResult>
{
    private readonly IDatabaseConnectionFactory _connectionFactory;

    public GetMessagesQueryHandler(IDatabaseConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<MessageSearchResult> Handle(GetMessagesQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var input = new QueryBuilder().BuildFrom(request);

        using var connection = await _connectionFactory.GetConnectionAndOpenAsync(cancellationToken).ConfigureAwait(false);
        var archivedMessages =
            await connection.QueryAsync<MessageInfo>(
                    input.SqlStatement,
                    input.Parameters)
                .ConfigureAwait(false);
        return new MessageSearchResult(archivedMessages.ToList().AsReadOnly());
    }
}
