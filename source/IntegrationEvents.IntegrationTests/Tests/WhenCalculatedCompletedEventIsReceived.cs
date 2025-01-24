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

using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.Core.Messaging.Communication.Subscriber;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FeatureFlag;
using Energinet.DataHub.EDI.BuildingBlocks.Tests.TestDoubles;
using Energinet.DataHub.EDI.IntegrationEvents.IntegrationTests.Fixture;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.IntegrationEvents.IntegrationTests.Tests;

[Collection(nameof(IntegrationEventsIntegrationTestCollection))]
public class WhenCalculatedCompletedEventIsReceived : IntegrationEventsTestBase
{
    public WhenCalculatedCompletedEventIsReceived(
        IntegrationEventsFixture integrationEventsFixture,
        ITestOutputHelper testOutputHelper)
        : base(integrationEventsFixture, testOutputHelper)
    {
        SetupServiceCollection();
    }

    [Fact]
    public async Task Given_CalculationCompletedV1_When_FeatureIsEnabled_Then_EventIsHandled()
    {
        var integrationEvent = new CalculationCompletedV1()
        {
            CalculationId = Guid.NewGuid().ToString(),
            CalculationVersion = 1,
            InstanceId = "instance",
            CalculationType = CalculationCompletedV1.Types.CalculationType.Aggregation,
        };

        var act = () => HavingReceivedAndHandledIntegrationEventAsync(integrationEvent);

        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task Given_CalculationCompletedV1_When_FeatureIsDisabled_Then_EventIsHandled()
    {
        FeatureFlagManagerStub.SetFeatureFlag(FeatureFlagName.EnqueueBrs023027MessagesViaProcessManager, true);

        var integrationEvent = new CalculationCompletedV1()
        {
            CalculationId = Guid.NewGuid().ToString(),
            CalculationVersion = 1,
            InstanceId = "instance",
            CalculationType = CalculationCompletedV1.Types.CalculationType.Aggregation,
        };

        var act = () => HavingReceivedAndHandledIntegrationEventAsync(integrationEvent);

        await act.Should().NotThrowAsync("the processer should not do anything");
    }

    private async Task HavingReceivedAndHandledIntegrationEventAsync(CalculationCompletedV1 calculationCompleted)
    {
        var integrationEventHandler = Services.GetService<IIntegrationEventHandler>();

        var integrationEvent = new IntegrationEvent(
            Guid.NewGuid(),
            CalculationCompletedV1.EventName,
            1,
            calculationCompleted);

        await integrationEventHandler!.HandleAsync(integrationEvent).ConfigureAwait(false);
    }
}
