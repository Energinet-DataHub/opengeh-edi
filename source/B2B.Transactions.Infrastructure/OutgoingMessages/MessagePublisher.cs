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
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using B2B.Transactions.Infrastructure.Configuration.Correlation;
using B2B.Transactions.OutgoingMessages;
using B2B.Transactions.Transactions;
using Energinet.DataHub.MessageHub.Client.DataAvailable;
using Energinet.DataHub.MessageHub.Model.Model;

namespace B2B.Transactions.Infrastructure.OutgoingMessages
{
    public class MessagePublisher
    {
        private readonly IDataAvailableNotificationSender _dataAvailableNotificationSender;
        private readonly ICorrelationContext _correlationContext;

        public MessagePublisher(IDataAvailableNotificationSender dataAvailableNotificationSender, ICorrelationContext correlationContext)
        {
            _dataAvailableNotificationSender = dataAvailableNotificationSender ?? throw new ArgumentNullException(nameof(dataAvailableNotificationSender));
            _correlationContext = correlationContext;
        }

        public async Task PublishAsync(ReadOnlyCollection<OutgoingMessage> unpublishedMessages)
        {
            if (unpublishedMessages == null) throw new ArgumentNullException(nameof(unpublishedMessages));
            foreach (var message in unpublishedMessages)
            {
                await _dataAvailableNotificationSender.SendAsync(
                    _correlationContext.Id,
                    CreateDataAvailableNotificationFrom(message)).ConfigureAwait(false);

                message.Published();
            }
        }

        private static DataAvailableNotificationDto CreateDataAvailableNotificationFrom(OutgoingMessage message)
        {
            return new DataAvailableNotificationDto(
                Guid.NewGuid(),
                new GlobalLocationNumberDto(message.RecipientId ?? string.Empty),
                new MessageTypeDto(string.Empty),
                DomainOrigin.MarketRoles,
                false,
                1,
                message.DocumentType);
        }
    }
}
