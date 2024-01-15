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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.IntegrationTests.Factories;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Xunit;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.OutgoingMessages;

public class WhenOutgoingMessagesAreCreatedTests : TestBase
{
    private readonly OutgoingMessageDtoBuilder _outgoingMessageDtoBuilder;
    private readonly IOutgoingMessagesClient _outgoingMessageClient;

    public WhenOutgoingMessagesAreCreatedTests(IntegrationTestFixture integrationTestFixture)
        : base(integrationTestFixture)
    {
        _outgoingMessageDtoBuilder = new OutgoingMessageDtoBuilder();
        _outgoingMessageClient = GetService<IOutgoingMessagesClient>();
    }

    [Fact]
    public async Task Outgoing_message_is_enqueued()
    {
        var message = _outgoingMessageDtoBuilder
            .WithReceiverNumber(SampleData.NewEnergySupplierNumber)
            .WithReceiverRole(MarketRole.EnergySupplier)
            .Build();
        await EnqueueMessage(message);

        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var sql = $"SELECT * FROM [dbo].[OutgoingMessages]";
        var result = await
            connection
            .QuerySingleOrDefaultAsync(sql);

        Assert.NotNull(result);
        Assert.Equal(result.DocumentType, DocumentType.NotifyAggregatedMeasureData.Name);
        Assert.Equal(result.ReceiverId, SampleData.NewEnergySupplierNumber);
        Assert.Equal(result.ReceiverRole, MarketRole.EnergySupplier.Name);
        Assert.Equal(result.SenderId, DataHubDetails.DataHubActorNumber.Value);
        Assert.Equal(result.SenderRole, MarketRole.MeteringDataAdministrator.Name);
        Assert.Equal(BusinessReason.BalanceFixing.Name, result.BusinessReason);
        Assert.NotNull(result.MessageRecord);
    }

    private async Task EnqueueMessage(OutgoingMessageDto message)
    {
        await _outgoingMessageClient.EnqueueAsync(message);
        await GetService<ActorMessageQueueContext>().SaveChangesAsync();
    }
}
