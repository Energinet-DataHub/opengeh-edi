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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using B2B.CimMessageAdapter.Errors;
using B2B.CimMessageAdapter.Messages;
using B2B.CimMessageAdapter.Schema;
using B2B.CimMessageAdapter.Transactions;
using Energinet.DataHub.Core.App.Common.Abstractions.Actor;

namespace B2B.CimMessageAdapter
{
    public class MessageReceiver
    {
        private readonly IMessageIds _messageIds;
        private readonly ITransactionQueueDispatcher _transactionQueueDispatcher;
        private readonly ITransactionIds _transactionIds;
        private readonly ISchemaProvider _schemaProvider;
        private readonly IActorContext _actorContext;

        public MessageReceiver(IMessageIds messageIds, ITransactionQueueDispatcher transactionQueueDispatcher, ITransactionIds transactionIds, ISchemaProvider schemaProvider, IActorContext actorContext)
        {
            _messageIds = messageIds ?? throw new ArgumentNullException(nameof(messageIds));
            _transactionQueueDispatcher = transactionQueueDispatcher ??
                                             throw new ArgumentNullException(nameof(transactionQueueDispatcher));
            _transactionIds = transactionIds;
            _schemaProvider = schemaProvider ?? throw new ArgumentNullException(nameof(schemaProvider));
            _actorContext = actorContext ?? throw new ArgumentNullException(nameof(actorContext));
        }

        public async Task<Result> ReceiveAsync(Stream message, string businessProcessType, string version)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var messageParser = new MessageParser(_schemaProvider);
            var messageParserResult =
                 await messageParser.ParseAsync(message, businessProcessType, version).ConfigureAwait(false);

            if (messageParserResult.Success == false)
            {
                return Result.Failure(messageParserResult.Errors.ToArray());
            }

            var messageHeader = messageParserResult.MessageHeader;

            var isAuthorized = await AuthorizeSenderAsync(messageHeader!.SenderId).ConfigureAwait(false);
            if (isAuthorized == false)
            {
                return Result.Failure(new SenderAuthorizationFailed());
            }

            var messageIdIsUnique = await CheckMessageIdAsync(messageHeader.MessageId).ConfigureAwait(false);
            if (messageIdIsUnique == false)
            {
                return Result.Failure(new DuplicateMessageIdDetected($"Message id '{messageHeader.MessageId}' is not unique"));
            }

            foreach (var marketActivityRecord in messageParserResult.MarketActivityRecords)
            {
                if (await CheckTransactionIdAsync(marketActivityRecord.Id).ConfigureAwait(false) == false)
                {
                    return Result.Failure(new DuplicateTransactionIdDetected(
                        $"Transaction id '{marketActivityRecord.Id}' is not unique and will not be processed."));
                }

                await AddToTransactionQueueAsync(CreateTransaction(messageHeader, marketActivityRecord)).ConfigureAwait(false);
            }

            await _transactionQueueDispatcher.CommitAsync().ConfigureAwait(false);
            return Result.Succeeded();
        }

        private static B2BTransaction CreateTransaction(MessageHeader messageHeader, MarketActivityRecord marketActivityRecord)
        {
            return B2BTransaction.Create(messageHeader, marketActivityRecord);
        }

        private Task<bool> CheckTransactionIdAsync(string transactionId)
        {
            if (transactionId == null) throw new ArgumentNullException(nameof(transactionId));
            return _transactionIds.TryStoreAsync(transactionId);
        }

        private Task AddToTransactionQueueAsync(B2BTransaction transaction)
        {
            return _transactionQueueDispatcher.AddAsync(transaction);
        }

        private Task<bool> CheckMessageIdAsync(string messageId)
        {
            if (messageId == null) throw new ArgumentNullException(nameof(messageId));
            return _messageIds.TryStoreAsync(messageId);
        }

        private Task<bool> AuthorizeSenderAsync(string senderId)
        {
            if (senderId == null) throw new ArgumentNullException(nameof(senderId));
            return Task.FromResult(_actorContext.CurrentActor!.Identifier.Equals(senderId, StringComparison.OrdinalIgnoreCase));
        }
    }
}
