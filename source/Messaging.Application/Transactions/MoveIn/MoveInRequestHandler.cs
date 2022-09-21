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
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Messaging.Application.Configuration;
using Messaging.Application.IncomingMessages.RequestChangeOfSupplier;
using Messaging.Application.OutgoingMessages;
using Messaging.Application.OutgoingMessages.Common;
using Messaging.Application.OutgoingMessages.Common.Reasons;
using Messaging.Application.OutgoingMessages.RejectRequestChangeOfSupplier;
using Messaging.Domain.MasterData.MarketEvaluationPoints;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.Transactions.MoveIn;
using NodaTime.Text;

namespace Messaging.Application.Transactions.MoveIn
{
    public class MoveInRequestHandler : IRequestHandler<RequestChangeOfSupplierTransaction, Unit>
    {
        private readonly IMoveInTransactionRepository _moveInTransactionRepository;
        private readonly IOutgoingMessageStore _outgoingMessageStore;
        private readonly IMarketActivityRecordParser _marketActivityRecordParser;
        private readonly IMoveInRequester _moveInRequester;
        private readonly IValidationErrorTranslator _validationErrorTranslator;
        private readonly IMarketEvaluationPointRepository _marketEvaluationPointRepository;

        public MoveInRequestHandler(
            IMoveInTransactionRepository moveInTransactionRepository,
            IOutgoingMessageStore outgoingMessageStore,
            IMarketActivityRecordParser marketActivityRecordParser,
            IMoveInRequester moveInRequester,
            IValidationErrorTranslator validationErrorTranslator,
            IMarketEvaluationPointRepository marketEvaluationPointRepository)
        {
            _moveInTransactionRepository = moveInTransactionRepository;
            _outgoingMessageStore = outgoingMessageStore;
            _marketActivityRecordParser = marketActivityRecordParser;
            _moveInRequester = moveInRequester;
            _validationErrorTranslator = validationErrorTranslator;
            _marketEvaluationPointRepository = marketEvaluationPointRepository;
        }

        public async Task<Unit> Handle(RequestChangeOfSupplierTransaction request, CancellationToken cancellationToken)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var marketEvaluationPoint =
                await _marketEvaluationPointRepository.GetByNumberAsync(request.MarketActivityRecord.MarketEvaluationPointId).ConfigureAwait(false);

            var transaction = new MoveInTransaction(
                request.MarketActivityRecord.Id,
                request.MarketActivityRecord.MarketEvaluationPointId,
                InstantPattern.General.Parse(request.MarketActivityRecord.EffectiveDate).GetValueOrThrow(),
                marketEvaluationPoint?.EnergySupplierNumber,
                request.Message.MessageId,
                request.MarketActivityRecord.EnergySupplierId ?? string.Empty,
                request.MarketActivityRecord.ConsumerId,
                request.MarketActivityRecord.ConsumerName,
                request.MarketActivityRecord.ConsumerIdType);

            if (string.IsNullOrEmpty(request.MarketActivityRecord.EnergySupplierId))
            {
                return await RejectInvalidRequestMessageAsync(transaction, request, "EnergySupplierIdIsEmpty")
                    .ConfigureAwait(false);
            }

            if (!IsEnergySupplierIdAndSenderIdAMatch(
                    request.MarketActivityRecord.EnergySupplierId,
                    request.Message.SenderId))
            {
               return await RejectInvalidRequestMessageAsync(transaction, request, "EnergySupplierDoesNotMatchSender").ConfigureAwait(false);
            }

            var businessProcessResult = await InvokeBusinessProcessAsync(transaction).ConfigureAwait(false);
            if (businessProcessResult.Success == false)
            {
                var reasons = await CreateReasonsFromAsync(businessProcessResult.ValidationErrors).ConfigureAwait(false);
                _outgoingMessageStore.Add(RejectMessageFrom(reasons, transaction, request));
                transaction.RejectedByBusinessProcess();
            }
            else
            {
                _outgoingMessageStore.Add(ConfirmMessageFrom(transaction, request));
                transaction.AcceptedByBusinessProcess(
                    businessProcessResult.ProcessId ?? throw new MoveInException("Business process id cannot be empty."),
                    request.MarketActivityRecord.MarketEvaluationPointId ?? throw new MoveInException("Market evaluation point number cannot be empty."));
            }

            _moveInTransactionRepository.Add(transaction);
            return Unit.Value;
        }

        private static bool IsEnergySupplierIdAndSenderIdAMatch(string? energySupplierId, string senderId)
        {
            return energySupplierId == senderId;
        }

        private static string GetConsumerIdType(string? consumerIdType)
        {
            var cprNumberTypeIdentifier = "ARR";
            var consumerType = string.Empty;
            if (!string.IsNullOrEmpty(consumerIdType))
            {
                consumerType =
                   consumerIdType.Equals(cprNumberTypeIdentifier, StringComparison.OrdinalIgnoreCase)
                        ? "CPR"
                        : "CVR";
            }

            return consumerType;
        }

        private static OutgoingMessage CreateOutgoingMessage(string id, DocumentType documentType, string processType, string receiverId, string marketActivityRecordPayload)
        {
            return new OutgoingMessage(
                documentType,
                receiverId,
                id,
                processType,
                MarketRoles.EnergySupplier,
                DataHubDetails.IdentificationNumber,
                MarketRoles.MeteringPointAdministrator,
                marketActivityRecordPayload);
        }

        private async Task<Unit> RejectInvalidRequestMessageAsync(MoveInTransaction transaction, RequestChangeOfSupplierTransaction request, string error)
        {
            var reasons = await CreateReasonsFromAsync(new Collection<string>() { error }).ConfigureAwait(false);
            _outgoingMessageStore.Add(RejectMessageFrom(reasons, transaction, request));
            transaction.RejectedByBusinessProcess();

            _moveInTransactionRepository.Add(transaction);
            return Unit.Value;
        }

        private Task<BusinessRequestResult> InvokeBusinessProcessAsync(MoveInTransaction transaction)
        {
            var businessProcess = new MoveInRequest(
                transaction.ConsumerName,
                transaction.NewEnergySupplierId,
                transaction.MarketEvaluationPointId,
                transaction.EffectiveDate.ToString(),
                transaction.ConsumerId,
                GetConsumerIdType(transaction.ConsumerIdType));
            return _moveInRequester.InvokeAsync(businessProcess);
        }

        private OutgoingMessage ConfirmMessageFrom(MoveInTransaction transaction, RequestChangeOfSupplierTransaction requestChangeOfSupplierTransaction)
        {
            var marketActivityRecord = new OutgoingMessages.ConfirmRequestChangeOfSupplier.MarketActivityRecord(
                Guid.NewGuid().ToString(),
                transaction.TransactionId,
                transaction.MarketEvaluationPointId);

            return CreateOutgoingMessage(
                transaction.StartedByMessageId,
                DocumentType.ConfirmRequestChangeOfSupplier,
                ProcessType.MoveIn.Code,
                requestChangeOfSupplierTransaction.Message.SenderId,
                _marketActivityRecordParser.From(marketActivityRecord));
        }

        private OutgoingMessage RejectMessageFrom(IReadOnlyCollection<Reason> reasons, MoveInTransaction transaction, RequestChangeOfSupplierTransaction requestChangeOfSupplierTransaction)
        {
            var marketActivityRecord = new OutgoingMessages.RejectRequestChangeOfSupplier.MarketActivityRecord(
                Guid.NewGuid().ToString(),
                transaction.TransactionId,
                transaction.MarketEvaluationPointId,
                reasons);

            return CreateOutgoingMessage(
                transaction.StartedByMessageId,
                DocumentType.RejectRequestChangeOfSupplier,
                ProcessType.MoveIn.Code,
                requestChangeOfSupplierTransaction.Message.SenderId,
                _marketActivityRecordParser.From(marketActivityRecord));
        }

        private Task<ReadOnlyCollection<Reason>> CreateReasonsFromAsync(IReadOnlyCollection<string> validationErrors)
        {
            return _validationErrorTranslator.TranslateAsync(validationErrors);
        }
    }
}
