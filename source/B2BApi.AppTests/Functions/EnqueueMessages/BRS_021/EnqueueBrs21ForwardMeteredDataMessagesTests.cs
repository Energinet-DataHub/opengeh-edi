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

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Energinet.DataHub.Core.FunctionApp.TestCommon.FunctionAppHost;
using Energinet.DataHub.Core.TestCommon;
using Energinet.DataHub.EDI.B2BApi.AppTests.Fixtures;
using Energinet.DataHub.EDI.B2BApi.Authentication;
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_021;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Tests.Logging;
using Energinet.DataHub.EDI.IntegrationTests.Infrastructure.Authentication.MarketActors;
using Energinet.DataHub.ProcessManager.Abstractions.Contracts;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_021.ForwardMeteredData;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_021.ForwardMeteredData.V1.Model;
using Energinet.DataHub.ProcessManager.Shared.Extensions;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;
using Xunit.Abstractions;
using PMValueTypes = Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Components.Datahub.ValueObjects;

namespace Energinet.DataHub.EDI.B2BApi.AppTests.Functions.EnqueueMessages.BRS_021;

[Collection(nameof(B2BApiAppCollectionFixture))]
public class EnqueueBrs21ForwardMeteredDataMessagesTests : IAsyncLifetime
{
    private readonly B2BApiAppFixture _fixture;

    public EnqueueBrs21ForwardMeteredDataMessagesTests(
        B2BApiAppFixture fixture,
        ITestOutputHelper testOutputHelper)
    {
        _fixture = fixture;
        _fixture.SetTestOutputHelper(testOutputHelper);
    }

    public async Task InitializeAsync()
    {
        _fixture.AppHostManager.ClearHostLog();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _fixture.SetTestOutputHelper(null!);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Given_EnqueueRejectedBrs021Message_When_MessageIsReceived_Then_RejectedMessagesIsEnqueued()
    {
        _fixture.EnsureAppHostUsesFeatureFlagValue(usePeekTimeSeriesMessages: true);

        // Arrange
        // => Given enqueue BRS-021 service bus message
        var actorId = Guid.NewGuid().ToString();
        var enqueueMessagesData = new MeteredDataForMeteringPointRejectedV1(
            "EventId",
            PMValueTypes.BusinessReason.PeriodicMetering,
            new MarketActorRecipient("1111111111111", PMValueTypes.ActorRole.GridAccessProvider),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new AcknowledgementV1(
                null,
                null,
                null,
                null,
                null,
                null,
                [],
                [],
                [],
                [],
                []));

        var enqueueActorMessages = new EnqueueActorMessagesV1
        {
            OrchestrationName = Brs_021_ForwardedMeteredData.Name,
            OrchestrationVersion = 1,
            OrchestrationStartedByActorId = actorId,
            Data = JsonSerializer.Serialize(enqueueMessagesData),
            DataType = nameof(MeteredDataForMeteringPointRejectedV1),
            DataFormat = EnqueueActorMessagesDataFormatV1.Json,
            OrchestrationInstanceId = Guid.NewGuid().ToString(),
        };

        // Act
        var serviceBusMessage = enqueueActorMessages.ToServiceBusMessage(
            subject: EnqueueActorMessagesV1.BuildServiceBusMessageSubject(enqueueActorMessages.OrchestrationName),
            idempotencyKey: Guid.NewGuid().ToString());

        // => When message is received
        var beforeOrchestrationCreated = DateTime.UtcNow;
        await _fixture.EdiTopicResource.SenderClient.SendMessageAsync(serviceBusMessage);

        // Assert
        using var assertionScope = new AssertionScope();

        var didFinish = await Awaiter.TryWaitUntilConditionAsync(
            () => _fixture.AppHostManager.CheckIfFunctionWasExecuted($"Functions.{nameof(EnqueueTrigger_Brs_021_Forward_Metered_Data_V1)}"),
            timeLimit: TimeSpan.FromSeconds(30));

        didFinish.Should()
            .BeTrue($"the {nameof(EnqueueTrigger_Brs_021_Forward_Metered_Data_V1)} should have been executed");

        var hostLog = _fixture.AppHostManager.GetHostLogSnapshot();

        hostLog.Should().ContainMatch("*Executing 'Functions.EnqueueTrigger_Brs_021_Forward_Metered_Data_V1'*");
        hostLog.Should().ContainMatch("*Received enqueue rejected message(s) for BRS 021*");
        hostLog.Should().ContainMatch("*INSERT INTO [dbo].[OutgoingMessages]*");
        hostLog.Should().ContainMatch("*Executed 'Functions.EnqueueTrigger_Brs_021_Forward_Metered_Data_V1' (Succeeded,*");

        var externalId = Guid.NewGuid().ToString();
        await _fixture.DatabaseManager.AddActorAsync(ActorNumber.Create("1111111111111"), externalId);

        using var request = new HttpRequestMessage(HttpMethod.Get, "api/peek/TimeSeries");

        var b2bToken = new JwtBuilder()
            .WithRole(ClaimsMap.RoleFrom(ActorRole.GridAccessProvider).Value)
            .WithClaim(ClaimsMap.ActorId, externalId)
            .CreateToken();

        request.Headers.Authorization = new AuthenticationHeaderValue("bearer", b2bToken);

        request.Content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
        var peekResponse = await _fixture.AppHostManager.HttpClient.SendAsync(request);
        await peekResponse.EnsureSuccessStatusCodeWithLogAsync(_fixture.TestLogger);
        peekResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await peekResponse.Content.ReadAsStringAsync()).Should().NotBeNullOrEmpty().And.Contain("Acknowledgement");

        hostLog = _fixture.AppHostManager.GetHostLogSnapshot();
        hostLog.Should().ContainMatch("*Executing 'Functions.PeekRequestListener'*");
        hostLog.Should().ContainMatch("*Executed 'Functions.PeekRequestListener' (Succeeded,*");
    }
}
