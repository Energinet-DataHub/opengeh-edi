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
using System.Threading.Tasks;
using B2B.Transactions.Configuration;
using B2B.Transactions.Infrastructure.Configuration.Correlation;
using B2B.Transactions.Infrastructure.DataAccess;
using B2B.Transactions.OutgoingMessages;
using Energinet.DataHub.MessageHub.Client.DataAvailable;
using Energinet.DataHub.MessageHub.Model.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace B2B.Transactions.Infrastructure.OutgoingMessages
{
    public class MessagePublisher
    {
        private readonly IDataAvailableNotificationSender _dataAvailableNotificationSender;
        private readonly ICorrelationContext _correlationContext;
        private readonly IOutgoingMessageStore _messageStore;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<MessagePublisher> _logger;

        public MessagePublisher(IDataAvailableNotificationSender dataAvailableNotificationSender, ICorrelationContext correlationContext, IOutgoingMessageStore messageStore, IServiceScopeFactory serviceScopeFactory, ILogger<MessagePublisher> logger)
        {
            _dataAvailableNotificationSender = dataAvailableNotificationSender ?? throw new ArgumentNullException(nameof(dataAvailableNotificationSender));
            _correlationContext = correlationContext ?? throw new ArgumentNullException(nameof(correlationContext));
            _messageStore = messageStore ?? throw new ArgumentNullException(nameof(messageStore));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task PublishAsync()
        {
            var unpublishedMessages = _messageStore.GetUnpublished();
            foreach (var message in unpublishedMessages)
            {
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    await SendNotificationAsync(message).ConfigureAwait(false);
                    await MarkMessageAsPublishedAsync(scope, message.Id).ConfigureAwait(false);
                }
#pragma warning disable CA1031 // Exception could be anything
                catch (Exception exception)
#pragma warning restore CA1031
                {
                    _logger.LogError(exception, $"Failed to publish message {message.Id}.");
                }
            }
        }

        private static async Task MarkMessageAsPublishedAsync(IServiceScope scope, Guid messageId)
        {
            var context = GetService<B2BContext>(scope);

            var storedMessage = await context.OutgoingMessages.FindAsync(messageId).ConfigureAwait(false);
            storedMessage.Published();

            await context.SaveChangesAsync().ConfigureAwait(false);
        }

        private static TService GetService<TService>(IServiceScope scope)
            where TService : notnull
        {
            return scope.ServiceProvider.GetRequiredService<TService>();
        }

        private static DataAvailableNotificationDto CreateDataAvailableNotificationFrom(OutgoingMessage message)
        {
            return new DataAvailableNotificationDto(
                message.Id,
                new GlobalLocationNumberDto(message.RecipientId),
                new MessageTypeDto(string.Empty),
                DomainOrigin.MarketRoles,
                false,
                1,
                message.DocumentType);
        }

        private async Task SendNotificationAsync(OutgoingMessage message)
        {
            await _dataAvailableNotificationSender.SendAsync(
                message.CorrelationId,
                CreateDataAvailableNotificationFrom(message)).ConfigureAwait(false);
        }
    }
}
