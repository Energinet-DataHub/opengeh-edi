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
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Messaging.Application.Common;
using Messaging.Application.Common.Reasons;
using Messaging.Application.Configuration;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Application.IncomingMessages;
using Messaging.Application.OutgoingMessages;
using Messaging.Application.OutgoingMessages.RejectRequestChangeOfSupplier;
using MarketActivityRecord = Messaging.Application.IncomingMessages.RequestChangeOfSupplier.MarketActivityRecord;

namespace Messaging.Application.Transactions.MoveIn
{
    public class MoveInRequestHandler
    {
        private readonly IMoveInTransactionRepository _moveInTransactionRepository;
        private readonly IOutgoingMessageStore _outgoingMessageStore;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICorrelationContext _correlationContext;
        private readonly IMarketActivityRecordParser _marketActivityRecordParser;
        private readonly IMoveInRequestAdapter _moveInRequestAdapter;
        private readonly IValidationErrorTranslator _validationErrorTranslator;

        public MoveInRequestHandler(
            IMoveInTransactionRepository moveInTransactionRepository,
            IOutgoingMessageStore outgoingMessageStore,
            IUnitOfWork unitOfWork,
            ICorrelationContext correlationContext,
            IMarketActivityRecordParser marketActivityRecordParser,
            IMoveInRequestAdapter moveInRequestAdapter,
            IValidationErrorTranslator validationErrorTranslator)
        {
            _moveInTransactionRepository = moveInTransactionRepository;
            _outgoingMessageStore = outgoingMessageStore;
            _unitOfWork = unitOfWork;
            _correlationContext = correlationContext;
            _marketActivityRecordParser = marketActivityRecordParser;
            _moveInRequestAdapter = moveInRequestAdapter;
            _validationErrorTranslator = validationErrorTranslator;
        }

        public async Task HandleAsync(IncomingMessage incomingMessage)
        {
            if (incomingMessage == null) throw new ArgumentNullException(nameof(incomingMessage));

            var acceptedTransaction = new MoveInTransaction(incomingMessage.MarketActivityRecord.Id);
            _moveInTransactionRepository.Add(acceptedTransaction);

            var businessProcessResult = await InvokeBusinessProcessAsync(incomingMessage).ConfigureAwait(false);
            if (businessProcessResult.Success == false)
            {
                var reasons = await CreateReasonsFromAsync(businessProcessResult.ValidationErrors).ConfigureAwait(false);
                _outgoingMessageStore.Add(RejectMessageFrom(incomingMessage, acceptedTransaction.TransactionId, reasons));
            }
            else
            {
                _outgoingMessageStore.Add(ConfirmMessageFrom(incomingMessage, acceptedTransaction.TransactionId));
            }

            await _unitOfWork.CommitAsync().ConfigureAwait(false);
        }

        private static string GetConsumerIdType(MarketActivityRecord marketActivityRecord)
        {
            var cprNumberTypeIdentifier = "ARR";
            var consumerType = string.Empty;
            if (marketActivityRecord.ConsumerIdType is not null)
            {
                consumerType =
                    marketActivityRecord.ConsumerIdType.Equals(cprNumberTypeIdentifier, StringComparison.OrdinalIgnoreCase)
                        ? "CPR"
                        : "CVR";
            }

            return consumerType;
        }

        private Task<BusinessRequestResult> InvokeBusinessProcessAsync(IncomingMessage incomingMessage)
        {
            var businessProcess = new MoveInRequest(
                incomingMessage.MarketActivityRecord.ConsumerName,
                incomingMessage.MarketActivityRecord.EnergySupplierId,
                incomingMessage.MarketActivityRecord.MarketEvaluationPointId,
                incomingMessage.MarketActivityRecord.EffectiveDate,
                incomingMessage.MarketActivityRecord.Id,
                incomingMessage.MarketActivityRecord.ConsumerId,
                GetConsumerIdType(incomingMessage.MarketActivityRecord));
            return _moveInRequestAdapter.InvokeAsync(businessProcess);
        }

        private OutgoingMessage ConfirmMessageFrom(IncomingMessage incomingMessage, string transactionId)
        {
            var messageId = Guid.NewGuid();
            var marketActivityRecord = new OutgoingMessages.ConfirmRequestChangeOfSupplier.MarketActivityRecord(
                messageId.ToString(),
                transactionId,
                incomingMessage.MarketActivityRecord.MarketEvaluationPointId);

            return CreateOutgoingMessage(
                incomingMessage.Id,
                "ConfirmRequestChangeOfSupplier",
                incomingMessage.Message.ProcessType,
                incomingMessage.Message.SenderId,
                _marketActivityRecordParser.From(marketActivityRecord));
        }

        private OutgoingMessage RejectMessageFrom(IncomingMessage incomingMessage, string transactionId, IReadOnlyCollection<Reason> reasons)
        {
            var messageId = Guid.NewGuid();
            var marketActivityRecord = new OutgoingMessages.RejectRequestChangeOfSupplier.MarketActivityRecord(
                messageId.ToString(),
                transactionId,
                incomingMessage.MarketActivityRecord.MarketEvaluationPointId,
                reasons);

            return CreateOutgoingMessage(
                incomingMessage.Id,
                "RejectRequestChangeOfSupplier",
                incomingMessage.Message.ProcessType,
                incomingMessage.Message.SenderId,
                _marketActivityRecordParser.From(marketActivityRecord));
        }

        private Task<ReadOnlyCollection<Reason>> CreateReasonsFromAsync(IReadOnlyCollection<string> validationErrors)
        {
            return _validationErrorTranslator.TranslateAsync(validationErrors);
        }

        private OutgoingMessage CreateOutgoingMessage(string id, string documentType, string processType, string receiverId, string marketActivityRecordPayload)
        {
            return new OutgoingMessage(
                documentType,
                receiverId,
                _correlationContext.Id,
                id,
                processType,
                MarketRoles.EnergySupplier,
                DataHubDetails.IdentificationNumber,
                MarketRoles.MeteringPointAdministrator,
                marketActivityRecordPayload);
        }
    }
}
