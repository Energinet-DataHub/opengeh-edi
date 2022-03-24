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
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using B2B.CimMessageAdapter.Errors;
using B2B.CimMessageAdapter.Messages;
using B2B.CimMessageAdapter.Schema;
using B2B.CimMessageAdapter.Transactions;
using Energinet.DataHub.Core.App.Common.Abstractions.Actor;

namespace B2B.CimMessageAdapter
{
    public class MessageReceiver
    {
        private const string MarketActivityRecordElementName = "MktActivityRecord";
        private const string HeaderElementName = "RequestChangeOfSupplier_MarketDocument";
        private readonly List<ValidationError> _errors = new();
        private readonly IMessageIds _messageIds;
        private readonly ITransactionQueueDispatcher _transactionQueueDispatcher;
        private readonly ITransactionIds _transactionIds;
        private readonly ISchemaProvider _schemaProvider;
        private readonly IActorContext _actorContext;
        private bool _hasInvalidHeaderValues;

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

            var xmlSchema = await _schemaProvider.GetSchemaAsync(businessProcessType, version).ConfigureAwait(true);
            if (xmlSchema is null)
            {
                return Result.Failure(new UnknownBusinessProcessTypeOrVersion(businessProcessType, version));
            }

            _hasInvalidHeaderValues = false;

            using (var reader = XmlReader.Create(message, CreateXmlReaderSettings(xmlSchema)))
            {
                try
                {
                    var messageHeader = await ExtractMessageHeaderAsync(reader).ConfigureAwait(false);

                    var isAuthorized = await AuthorizeSenderAsync(messageHeader.SenderId).ConfigureAwait(false);
                    if (isAuthorized == false)
                    {
                        return Result.Failure(new SenderAuthorizationFailed());
                    }

                    var messageIdIsUnique = await CheckMessageIdAsync(messageHeader.MessageId).ConfigureAwait(false);
                    if (messageIdIsUnique == false)
                    {
                        return Result.Failure(new DuplicateMessageIdDetected($"Message id '{messageHeader.MessageId}' is not unique"));
                    }

                    await foreach (var marketActivityRecord in MarketActivityRecordsFromAsync(reader))
                    {
                        if (await CheckTransactionIdAsync(marketActivityRecord.Id).ConfigureAwait(false) == false)
                        {
                            return Result.Failure(new DuplicateTransactionIdDetected(
                                $"Transaction id '{marketActivityRecord.Id}' is not unique and will not be processed."));
                        }

                        await AddToTransactionQueueAsync(CreateTransaction(messageHeader, marketActivityRecord)).ConfigureAwait(false);
                    }
                }
                catch (XmlException exception)
                {
                    return InvalidXmlFailure(exception);
                }
                catch (ObjectDisposedException generalException)
                {
                    return InvalidXmlFailure(generalException);
                }
            }

            if (_hasInvalidHeaderValues == false)
            {
                await _transactionQueueDispatcher.CommitAsync().ConfigureAwait(false);
            }

            return _errors.Count == 0 ? Result.Succeeded() : Result.Failure(_errors.ToArray());
        }

        #pragma warning disable

        private static B2BTransaction CreateTransaction(MessageHeader messageHeader, MarketActivityRecord marketActivityRecord)
        {
            return B2BTransaction.Create(messageHeader, marketActivityRecord);
        }

        private static Result InvalidXmlFailure(Exception exception)
        {
            return Result.Failure(InvalidMessageStructure.From(exception));
        }

        private static async IAsyncEnumerable<MarketActivityRecord> MarketActivityRecordsFromAsync(XmlReader reader)
        {
            var id = string.Empty;
            var marketEvaluationPointId = string.Empty;
            var energySupplierId = string.Empty;
            var balanceResponsibleId = string.Empty;
            var consumerId = string.Empty;
            var consumerName = string.Empty;
            var effectiveDate = string.Empty;

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                if (EndOfMarketActivityRecord(reader))
                {
                    var marketActivityRecord = new MarketActivityRecord()
                    {
                        Id = id,
                        ConsumerName = consumerName,
                        ConsumerId = consumerId,
                        MarketEvaluationPointId = marketEvaluationPointId,
                        EnergySupplierId = energySupplierId,
                        EffectiveDate = effectiveDate,
                        BalanceResponsibleId =
                            balanceResponsibleId,
                    };

                    id = string.Empty;
                    marketEvaluationPointId = string.Empty;
                    energySupplierId = string.Empty;
                    balanceResponsibleId = string.Empty;
                    consumerId = string.Empty;
                    consumerName = string.Empty;
                    effectiveDate = string.Empty;

                    yield return marketActivityRecord;
                }

                if (reader.NodeType == XmlNodeType.Element && reader.SchemaInfo?.Validity == XmlSchemaValidity.Invalid)
                {
                    await MoveToEndOfMarketActivityRecordAsync(reader).ConfigureAwait(false);
                }
                else
                {
                    TryExtractValueFrom("mRID", reader, (value) => id = value);
                    TryExtractValueFrom("marketEvaluationPoint.mRID", reader, (value) => marketEvaluationPointId = value);
                    TryExtractValueFrom("marketEvaluationPoint.energySupplier_MarketParticipant.mRID", reader, (value) => energySupplierId = value);
                    TryExtractValueFrom("marketEvaluationPoint.balanceResponsibleParty_MarketParticipant.mRID", reader, (value) => balanceResponsibleId = value);
                    TryExtractValueFrom("marketEvaluationPoint.customer_MarketParticipant.mRID", reader, (value) => consumerId = value);
                    TryExtractValueFrom("marketEvaluationPoint.customer_MarketParticipant.name", reader, (value) => consumerName = value);
                    TryExtractValueFrom("start_DateAndOrTime.dateTime", reader, (value) => effectiveDate = value);
                }
            }
        }

        private static async Task MoveToEndOfMarketActivityRecordAsync(XmlReader reader)
        {
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                if (EndOfMarketActivityRecord(reader))
                {
                    break;
                }
            }
        }

        private static bool EndOfMarketActivityRecord(XmlReader reader)
        {
            return reader.NodeType == XmlNodeType.EndElement &&
                   reader.LocalName.Equals(MarketActivityRecordElementName, StringComparison.OrdinalIgnoreCase);
        }

        private static bool StartOfMessageHeader(XmlReader reader)
        {
            return reader.NodeType == XmlNodeType.Element &&
                   reader.LocalName.Equals(HeaderElementName, StringComparison.OrdinalIgnoreCase);
        }

        private static bool StartOfMarketActivityRecord(XmlReader reader)
        {
            return reader.NodeType == XmlNodeType.Element &&
                   reader.LocalName.Equals(MarketActivityRecordElementName, StringComparison.OrdinalIgnoreCase);
        }

        private static void TryExtractValueFrom(string elementName, XmlReader reader, Func<string, string> variable)
        {
            if (reader.LocalName.Equals(elementName, StringComparison.OrdinalIgnoreCase) && reader.NodeType == XmlNodeType.Element)
            {
                variable(reader.ReadElementString());
            }
        }

        private static async Task<MessageHeader> ExtractMessageHeaderAsync(XmlReader reader)
        {
            var messageId = string.Empty;
            var processType = string.Empty;
            var senderId = string.Empty;
            var senderRole = string.Empty;
            var receiverId = string.Empty;
            var receiverRole = string.Empty;
            var createdAt = string.Empty;

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                if (StartOfMessageHeader(reader))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        if (StartOfMarketActivityRecord(reader))
                        {
                            break;
                        }

                        TryExtractValueFrom("mRID", reader, value => messageId = value);
                        TryExtractValueFrom("process.processType", reader, value => processType = value);
                        TryExtractValueFrom("sender_MarketParticipant.mRID", reader, value => senderId = value);
                        TryExtractValueFrom("sender_MarketParticipant.marketRole.type", reader, value => senderRole = value);
                        TryExtractValueFrom("receiver_MarketParticipant.mRID", reader, value => receiverId = value);
                        TryExtractValueFrom("receiver_MarketParticipant.marketRole.type", reader, value => receiverRole = value);
                        TryExtractValueFrom("createdDateTime", reader, value => createdAt = value);
                    }

                    break;
                }
            }

            return new MessageHeader(messageId, processType, senderId, senderRole, receiverId, receiverRole, createdAt);
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

        private XmlReaderSettings CreateXmlReaderSettings(XmlSchema xmlSchema)
        {
            var settings = new XmlReaderSettings
            {
                Async = true,
                ValidationType = ValidationType.Schema,
                ValidationFlags = XmlSchemaValidationFlags.ProcessInlineSchema |
                                  XmlSchemaValidationFlags.ReportValidationWarnings,
            };

            settings.Schemas.Add(xmlSchema);
            settings.ValidationEventHandler += OnValidationError;
            return settings;
        }

        private void OnValidationError(object? sender, ValidationEventArgs arguments)
        {
            var message =
                $"XML schema validation error at line {arguments.Exception.LineNumber}, position {arguments.Exception.LinePosition}: {arguments.Message}.";
            _errors.Add(InvalidMessageStructure.From(message));
        }

        private Task<bool> AuthorizeSenderAsync(string senderId)
        {
            if (senderId == null) throw new ArgumentNullException(nameof(senderId));
            return Task.FromResult(_actorContext.CurrentActor.Identifier.Equals(senderId, StringComparison.OrdinalIgnoreCase));
        }
    }
}
