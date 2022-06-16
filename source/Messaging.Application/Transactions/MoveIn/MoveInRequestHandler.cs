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
using Messaging.Domain.MasterData.MarketEvaluationPoints;
using NodaTime.Text;
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
        private readonly IMoveInRequester _moveInRequester;
        private readonly IValidationErrorTranslator _validationErrorTranslator;
        private readonly IMarketEvaluationPointRepository _marketEvaluationPointRepository;

        public MoveInRequestHandler(
            IMoveInTransactionRepository moveInTransactionRepository,
            IOutgoingMessageStore outgoingMessageStore,
            IUnitOfWork unitOfWork,
            ICorrelationContext correlationContext,
            IMarketActivityRecordParser marketActivityRecordParser,
            IMoveInRequester moveInRequester,
            IValidationErrorTranslator validationErrorTranslator,
            IMarketEvaluationPointRepository marketEvaluationPointRepository)
        {
            _moveInTransactionRepository = moveInTransactionRepository;
            _outgoingMessageStore = outgoingMessageStore;
            _unitOfWork = unitOfWork;
            _correlationContext = correlationContext;
            _marketActivityRecordParser = marketActivityRecordParser;
            _moveInRequester = moveInRequester;
            _validationErrorTranslator = validationErrorTranslator;
            _marketEvaluationPointRepository = marketEvaluationPointRepository;
        }

        public async Task HandleAsync(IncomingMessage incomingMessage)
        {
            if (incomingMessage == null) throw new ArgumentNullException(nameof(incomingMessage));

            var marketEvaluationPoint =
                await _marketEvaluationPointRepository.GetByNumberAsync(incomingMessage.MarketActivityRecord.MarketEvaluationPointId).ConfigureAwait(false);

            var transaction = new MoveInTransaction(
                incomingMessage.MarketActivityRecord.Id,
                incomingMessage.MarketActivityRecord.MarketEvaluationPointId,
                InstantPattern.General.Parse(incomingMessage.MarketActivityRecord.EffectiveDate).GetValueOrThrow(),
                marketEvaluationPoint?.EnergySupplierNumber,
                incomingMessage.Message.MessageId,
                incomingMessage.Message.SenderId,
                incomingMessage.MarketActivityRecord.ConsumerId!,
                incomingMessage.MarketActivityRecord.ConsumerName!);

            var businessProcessResult = await InvokeBusinessProcessAsync(incomingMessage, transaction).ConfigureAwait(false);
            if (businessProcessResult.Success == false)
            {
                var reasons = await CreateReasonsFromAsync(businessProcessResult.ValidationErrors).ConfigureAwait(false);
                _outgoingMessageStore.Add(RejectMessageFrom(reasons, transaction));
            }
            else
            {
                _outgoingMessageStore.Add(ConfirmMessageFrom(transaction));
            }

            transaction.Start(businessProcessResult);
            _moveInTransactionRepository.Add(transaction);
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

        private Task<BusinessRequestResult> InvokeBusinessProcessAsync(IncomingMessage incomingMessage, MoveInTransaction transaction)
        {
            var businessProcess = new MoveInRequest(
                transaction.ConsumerName,
                transaction.NewEnergySupplierId,
                transaction.MarketEvaluationPointId,
                transaction.EffectiveDate.ToString(),
                transaction.ConsumerId,
                GetConsumerIdType(incomingMessage.MarketActivityRecord));
            return _moveInRequester.InvokeAsync(businessProcess);
        }

        private OutgoingMessage ConfirmMessageFrom(MoveInTransaction transaction)
        {
            var marketActivityRecord = new OutgoingMessages.ConfirmRequestChangeOfSupplier.MarketActivityRecord(
                Guid.NewGuid().ToString(),
                transaction.TransactionId,
                transaction.MarketEvaluationPointId);

            return CreateOutgoingMessage(
                transaction.StartedByMessageId,
                ProcessType.MoveIn.Confirm.DocumentType,
                ProcessType.MoveIn.Code,
                transaction.NewEnergySupplierId,
                _marketActivityRecordParser.From(marketActivityRecord),
                ProcessType.MoveIn.Confirm.BusinessReasonCode);
        }

        private OutgoingMessage RejectMessageFrom(IReadOnlyCollection<Reason> reasons, MoveInTransaction transaction)
        {
            var marketActivityRecord = new OutgoingMessages.RejectRequestChangeOfSupplier.MarketActivityRecord(
                Guid.NewGuid().ToString(),
                transaction.TransactionId,
                transaction.MarketEvaluationPointId,
                reasons);

            return CreateOutgoingMessage(
                transaction.StartedByMessageId,
                ProcessType.MoveIn.Reject.DocumentType,
                ProcessType.MoveIn.Code,
                transaction.NewEnergySupplierId,
                _marketActivityRecordParser.From(marketActivityRecord),
                ProcessType.MoveIn.Reject.BusinessReasonCode);
        }

        private Task<ReadOnlyCollection<Reason>> CreateReasonsFromAsync(IReadOnlyCollection<string> validationErrors)
        {
            return _validationErrorTranslator.TranslateAsync(validationErrors);
        }

        private OutgoingMessage CreateOutgoingMessage(string id, string documentType, string processType, string receiverId, string marketActivityRecordPayload, string reasonCode)
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
                marketActivityRecordPayload,
                reasonCode);
        }
    }
}
