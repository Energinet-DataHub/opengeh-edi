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
using Messaging.Application.IncomingMessages.RequestChangeOfSupplier;
using Messaging.Application.OutgoingMessages.Common.Reasons;
using Messaging.Domain.Actors;
using Messaging.Domain.MasterData.MarketEvaluationPoints;
using Messaging.Domain.OutgoingMessages.RejectRequestChangeOfSupplier;
using Messaging.Domain.Transactions;
using Messaging.Domain.Transactions.MoveIn;
using NodaTime.Text;

namespace Messaging.Application.Transactions.MoveIn
{
    public class MoveInRequestHandler : IRequestHandler<RequestChangeOfSupplierTransaction, Unit>
    {
        private readonly IMoveInTransactionRepository _moveInTransactionRepository;
        private readonly IMoveInRequester _moveInRequester;
        private readonly IValidationErrorTranslator _validationErrorTranslator;
        private readonly IMarketEvaluationPointRepository _marketEvaluationPointRepository;

        public MoveInRequestHandler(
            IMoveInTransactionRepository moveInTransactionRepository,
            IMoveInRequester moveInRequester,
            IValidationErrorTranslator validationErrorTranslator,
            IMarketEvaluationPointRepository marketEvaluationPointRepository)
        {
            _moveInTransactionRepository = moveInTransactionRepository;
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
                TransactionId.Create(request.MarketActivityRecord.Id),
                ActorProvidedId.Create(request.MarketActivityRecord.Id),
                request.MarketActivityRecord.MarketEvaluationPointId,
                InstantPattern.General.Parse(request.MarketActivityRecord.EffectiveDate).GetValueOrThrow(),
                marketEvaluationPoint?.EnergySupplierNumber?.Value,
                request.Message.MessageId,
                request.MarketActivityRecord.EnergySupplierId ?? string.Empty,
                request.MarketActivityRecord.ConsumerId,
                request.MarketActivityRecord.ConsumerName,
                request.MarketActivityRecord.ConsumerIdType,
                ActorNumber.Create(request.Message.SenderId));

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
                transaction.Reject(reasons);
            }
            else
            {
                transaction.Accept(
                    businessProcessResult.ProcessId ?? throw new MoveInException("Business process id cannot be empty."));
            }

            _moveInTransactionRepository.Add(transaction);
            return Unit.Value;
        }

        private static bool IsEnergySupplierIdAndSenderIdAMatch(string? energySupplierId, string senderId)
        {
            return energySupplierId == senderId;
        }

        private async Task<Unit> RejectInvalidRequestMessageAsync(MoveInTransaction transaction, RequestChangeOfSupplierTransaction request, string error)
        {
            var reasons = await CreateReasonsFromAsync(new Collection<string>() { error }).ConfigureAwait(false);
            transaction.Reject(reasons);

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
                transaction.ConsumerId);
            return _moveInRequester.InvokeAsync(businessProcess);
        }

        private Task<ReadOnlyCollection<Reason>> CreateReasonsFromAsync(IReadOnlyCollection<string> validationErrors)
        {
            return _validationErrorTranslator.TranslateAsync(validationErrors);
        }
    }
}
