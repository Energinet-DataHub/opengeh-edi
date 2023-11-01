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

using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Energinet.DataHub.EDI.ActorMessageQueue.Domain.OutgoingMessages.Queueing;
using Energinet.DataHub.EDI.Application.Configuration.DataAccess;
using Energinet.DataHub.EDI.Common;
using Energinet.DataHub.EDI.Common.Actors;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.DataAccess;
using MediatR;
using Xunit;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.OutgoingMessages;

public class WhenOutgoingMessagesAreCreatedTests : ProcessTestBase
{
    private readonly RequestAggregatedMeasuredDataProcessInvoker _requestAggregatedMeasuredDataProcessInvoker;

    public WhenOutgoingMessagesAreCreatedTests(ProcessDatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _requestAggregatedMeasuredDataProcessInvoker =
            new RequestAggregatedMeasuredDataProcessInvoker(GetService<IMediator>(), GetService<ProcessContext>());
    }

    [Fact]
    public async Task Outgoing_message_is_enqueued()
    {
        await _requestAggregatedMeasuredDataProcessInvoker.HasBeenAcceptedAsync();

        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var sql = $"SELECT * FROM [dbo].[OutgoingMessages]";
        var result = await
            connection
            .QuerySingleOrDefaultAsync(sql);

        Assert.NotNull(result);
        Assert.Equal(result.DocumentType, DocumentType.NotifyAggregatedMeasureData.Name);
        Assert.Equal(result.ReceiverId, SampleData.NewEnergySupplierNumber);
        Assert.Equal(result.ReceiverRole, MarketRole.EnergySupplier.Name);
        Assert.Equal(result.SenderId, DataHubDetails.IdentificationNumber.Value);
        Assert.Equal(result.SenderRole, MarketRole.MeteringDataAdministrator.Name);
        Assert.Equal(BusinessReason.PreliminaryAggregation.Name, result.BusinessReason);
        Assert.NotNull(result.MessageRecord);
    }
}
