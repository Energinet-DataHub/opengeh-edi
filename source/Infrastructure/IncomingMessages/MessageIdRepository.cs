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
using Energinet.DataHub.EDI.Domain.Actors;
using Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages;
using Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages.Exceptions;
using Energinet.DataHub.EDI.Infrastructure.Configuration.DataAccess;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.Infrastructure.IncomingMessages
{
    public class MessageIdRepository : IMessageIdRepository
    {
        private readonly B2BContext _b2BContext;
        private readonly IDatabaseConnectionFactory _connectionFactory;
        private readonly ILogger _logger;

        public MessageIdRepository(
            B2BContext b2BContext,
            IDatabaseConnectionFactory connectionFactory,
            ILogger<MessageIdRepository> logger)
        {
            _b2BContext = b2BContext;
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _logger = logger;
        }

        public async Task StoreAsync(string senderNumber, string messageId, CancellationToken cancellationToken)
        {
            if (senderNumber == null) throw new ArgumentNullException(nameof(senderNumber));

            await _b2BContext.MessageIds.AddAsync(
                    new MessageIdForSender(messageId, senderNumber), cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<bool> MessageIdExistsAsync(
            string senderNumber,
            string messageId,
            CancellationToken cancellationToken)
        {
            if (senderNumber == null) throw new ArgumentNullException(nameof(senderNumber));

            var message = await GetMessageFromDbAsync(senderNumber, messageId, cancellationToken).ConfigureAwait(false)
                              ?? GetMessageFromInMemoryCollection(senderNumber, messageId);

            return message != null;
        }

        private MessageIdForSender? GetMessageFromInMemoryCollection(string senderNumber, string messageId)
        {
            return _b2BContext.MessageIds.Local
                .FirstOrDefault(x => x.MessageId == messageId && x.SenderId == senderNumber);
        }

        private async Task<MessageIdForSender?> GetMessageFromDbAsync(
            string senderId,
            string messageId,
            CancellationToken cancellationToken)
        {
            return await _b2BContext.MessageIds
                .FirstOrDefaultAsync(
                    messageIdForSender => messageIdForSender.MessageId == messageId
                                              && messageIdForSender.SenderId == senderId,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
