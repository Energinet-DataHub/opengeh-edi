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
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Energinet.DataHub.EDI.Application.Configuration.DataAccess;
using Energinet.DataHub.EDI.Application.OutgoingMessages;
using Energinet.DataHub.EDI.Domain.Actors;
using Energinet.DataHub.EDI.Domain.Common;
using Energinet.DataHub.EDI.Domain.Documents;

namespace Energinet.DataHub.EDI.Infrastructure.OutgoingMessages.OutgoingMessagesConfiguration
{
    public class OutgoingMessagesConfigurationRepository : IOutgoingMessagesConfigurationRepository
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;

        public OutgoingMessagesConfigurationRepository(IDatabaseConnectionFactory databaseConnectionFactory)
        {
            _databaseConnectionFactory = databaseConnectionFactory;
        }

        public async Task<DocumentFormat> GetDocumentFormatAsync(ActorNumber actorNumber, MarketRole marketRole, DocumentType documentType)
        {
            ArgumentNullException.ThrowIfNull(actorNumber);
            ArgumentNullException.ThrowIfNull(marketRole);
            ArgumentNullException.ThrowIfNull(documentType);
            using var cancel = new CancellationTokenSource();
            using var connection = await _databaseConnectionFactory.GetConnectionAndOpenAsync(cancel.Token).ConfigureAwait(false);
            var res = await connection.ExecuteScalarAsync<string>(
                    "SELECT OutputFormat FROM [dbo].[OutgoingMessagesConfiguration] WHERE [ActorNumber] = @Number and [ActorRole] = @Role and [DocumentType] = @DocumentType",
                    new { Number = actorNumber.Value, Role = marketRole.Name, DocumentType = documentType.Name }).ConfigureAwait(false);
            return res is not null ? EnumerationType.FromName<DocumentFormat>(res) : DocumentFormat.Xml;
        }
    }
}
