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
using Energinet.DataHub.EDI.ActorMessageQueue.Contracts;
using Energinet.DataHub.EDI.Common;
using Energinet.DataHub.EDI.Common.Actors;
using Energinet.DataHub.EDI.Process.Domain.Transactions;
using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData.ProcessEvents;
using MediatR;

namespace Energinet.DataHub.EDI.IntegrationTests.Infrastructure.Configuration.InternalCommands;

public class TestCreateOutgoingCommandHandler : IRequestHandler<TestCreateOutgoingMessageCommand, Unit>
{
    private readonly IMediator _mediator;

    public TestCreateOutgoingCommandHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<Unit> Handle(TestCreateOutgoingMessageCommand request, CancellationToken cancellationToken)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        for (int i = 0; i < request.NumberOfOutgoingMessages; i++)
        {
            var message = new OutgoingMessageDto(DocumentType.NotifyAggregatedMeasureData, ActorNumber.Create("1234567891234"), ProcessId.New().Id, BusinessReason.BalanceFixing.Name, MarketRole.EnergySupplier, ActorNumber.Create("1234567891234"), MarketRole.MeteringDataAdministrator, "data");

            await _mediator.Publish(new EnqueueMessageEvent(message), cancellationToken).ConfigureAwait(false);
        }

        return await Unit.Task;
    }
}
