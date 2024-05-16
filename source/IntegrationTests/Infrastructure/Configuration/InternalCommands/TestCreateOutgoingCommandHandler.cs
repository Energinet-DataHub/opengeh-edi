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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Energinet.DataHub.EDI.Process.Domain.Transactions;
using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData.ProcessEvents;
using MediatR;
using NodaTime.Text;

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
        ArgumentNullException.ThrowIfNull(request);
        for (int i = 0; i < request.NumberOfOutgoingMessages; i++)
        {
            var message = AcceptedEnergyResultMessageDto.Create(
                ActorNumber.Create("1234567891234"),
                ActorRole.EnergySupplier,
                ActorNumber.Create("1234567891234"),
                ActorRole.EnergySupplier,
                ProcessId.New().Id,
                EventId.From(Guid.NewGuid()),
                "123",
                MeteringPointType.Consumption.Name,
                SettlementMethod.Flex.Name,
                MeasurementUnit.Kwh.Name,
                Resolution.QuarterHourly.Name,
                "1234567891234",
                null,
                new Period(InstantPattern.ExtendedIso.Parse("2021-01-01T00:00:00Z").Value, InstantPattern.ExtendedIso.Parse("2021-01-01T00:15:00Z").Value),
                new List<AcceptedEnergyResultMessagePoint>
                {
                    new(1, 1, CalculatedQuantityQuality.Incomplete, "2021-01-01T00:00:00Z"),
                },
                BusinessReason.BalanceFixing.Name,
                1,
                TransactionId.New(),
                null,
                MessageId.New());
            await _mediator.Publish(new EnqueueAcceptedEnergyResultMessageEvent(message), cancellationToken).ConfigureAwait(false);
        }

        return await Unit.Task;
    }
}
