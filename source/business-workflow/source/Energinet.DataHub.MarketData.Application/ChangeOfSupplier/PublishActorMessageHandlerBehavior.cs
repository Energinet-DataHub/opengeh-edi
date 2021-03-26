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
using Energinet.DataHub.MarketData.Application.ChangeOfSupplier.ActorMessages;
using Energinet.DataHub.MarketData.Application.ChangeOfSupplier.MasterData;
using Energinet.DataHub.MarketData.Application.Common;
using MediatR;

namespace Energinet.DataHub.MarketData.Application.ChangeOfSupplier
{
    public class PublishActorMessageHandlerBehavior : IPipelineBehavior<RequestChangeOfSupplier, RequestChangeOfSupplierResult>
    {
        private readonly IActorMessagePublisher _messagePublisher;
        private IMediator _mediator;

        public PublishActorMessageHandlerBehavior(IActorMessagePublisher messagePublisher, IMediator mediator)
        {
            _messagePublisher = messagePublisher ?? throw new ArgumentNullException(nameof(messagePublisher));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public async Task<RequestChangeOfSupplierResult> Handle(RequestChangeOfSupplier command, CancellationToken cancellationToken, RequestHandlerDelegate<RequestChangeOfSupplierResult> next)
        {
            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            if (next is null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            var grouping = Guid.NewGuid();
            var result = await next().ConfigureAwait(false);
            if (result.Succeeded)
            {
                await PublishAcceptedMessageAsync(command, grouping).ConfigureAwait(false);
                await PublishMeteringPointMasterDataAsync(command, grouping);
            }
            else
            {
                await PublishRejectionMessageAsync(command, result, grouping).ConfigureAwait(false);
            }

            return result;
        }

        private Task PublishAcceptedMessageAsync(RequestChangeOfSupplier command, Guid grouping)
        {
            // TODO: <INSERT MESSAGE ID> will be replaced in another PR
            var message = new RequestChangeOfSupplierApproved("<INSERT MESSAGE ID>", command.Transaction.MRID, command.MarketEvaluationPoint.MRid, command.EnergySupplier.MRID!, command.StartDate);
            return SendMessageAsync(message, command.EnergySupplier.MRID!, grouping, 1);
        }

        private Task PublishRejectionMessageAsync(RequestChangeOfSupplier command, RequestChangeOfSupplierResult result, Guid grouping)
        {
            // TODO: <INSERT MESSAGE ID> will be replaced in another PR
            var message = new RequestChangeOfSupplierRejected("<INSERT MESSAGE ID>", command.Transaction.MRID, command.MarketEvaluationPoint.MRid, result.Errors!);
            return SendMessageAsync(message, command.EnergySupplier.MRID!, grouping, 1);
        }

        private async Task PublishMeteringPointMasterDataAsync(RequestChangeOfSupplier command, Guid grouping)
        {
            var queryMasterData = new QueryMasterData { GsrnNumber = command.MarketEvaluationPoint.MRid };
            var masterData = await _mediator.Send(queryMasterData, CancellationToken.None).ConfigureAwait(false);
            await SendMessageAsync(masterData, command.EnergySupplier.MRID!, grouping, 2);
        }

        private Task SendMessageAsync<TMessage>(TMessage message, string recipient, Guid grouping, int priority)
        {
            return _messagePublisher.PublishAsync(message, recipient, grouping, priority);
        }
    }
}
