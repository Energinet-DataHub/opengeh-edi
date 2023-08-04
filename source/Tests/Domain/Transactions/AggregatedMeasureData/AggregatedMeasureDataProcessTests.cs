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

using System.Linq;
using Domain.Actors;
using Domain.Transactions;
using Domain.Transactions.AggregatedMeasureData;
using Domain.Transactions.AggregatedMeasureData.Events;
using NodaTime.Text;
using Xunit;

namespace Tests.Domain.Transactions.AggregatedMeasureData;

public class AggregatedMeasureDataProcessTests
{
    [Fact]
    public void Process_is_started()
    {
        var process = CreateProcess();

        var startedEvent = process.DomainEvents.FirstOrDefault(e => e is AggregatedMeasureProcessWasStarted) as AggregatedMeasureProcessWasStarted;
        Assert.NotNull(startedEvent);
        Assert.Equal(SampleData.ProcessId, startedEvent?.ProcessId.Id);
    }

    [Fact]
    public void Process_is_set_to_sent()
    {
        var process = CreateProcess();
        process.WholesaleIsNotifiedOfRequest();
        // currently our state is private, hence we can not test on anything here.
        // As of right now!
    }

    [Fact]
    public void Process_can_not_be_send_twice()
    {
        var process = CreateProcess();
        process.WholesaleIsNotifiedOfRequest();

        Assert.Throws<AggregatedMeasureDataException>(() => process.WholesaleIsNotifiedOfRequest());
    }

    private static AggregatedMeasureDataProcess CreateProcess()
    {
        return AggregatedMeasureDataProcess.Create(
            ProcessId.Create(SampleData.ProcessId),
            BusinessTransactionId.Create(SampleData.BusinessTransactionId),
            ActorNumber.Create(SampleData.RequestedByActorId),
            SampleData.RequestedByActorRole,
            SampleData.BusinessReason,
            SampleData.SettlementVersion,
            SampleData.MeteringPointType,
            SampleData.SettlementMethod,
            InstantPattern.General.Parse(SampleData.StartOfPeriod).GetValueOrThrow(),
            InstantPattern.General.Parse(SampleData.EndOfPeriod).GetValueOrThrow(),
            SampleData.MeteringGridAreaDomainId,
            SampleData.EnergySupplierId,
            SampleData.BalanceResponsibleId);
    }
}
