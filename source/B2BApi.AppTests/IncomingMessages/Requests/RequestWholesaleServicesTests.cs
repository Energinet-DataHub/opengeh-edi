﻿// Copyright 2020 Energinet DataHub A/S
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

using Energinet.DataHub.Core.FunctionApp.TestCommon.ServiceBus.ListenerMock;
using Energinet.DataHub.EDI.B2BApi.AppTests.Fixtures;
using Energinet.DataHub.EDI.B2BApi.AppTests.Fixtures.Extensions;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Tests.Logging;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_026_028.BRS_028;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.B2BApi.AppTests.IncomingMessages.Requests;

[Collection(nameof(B2BApiAppCollectionFixture))]
public class RequestWholesaleServicesTests : IAsyncLifetime
{
    public RequestWholesaleServicesTests(
        B2BApiAppFixture fixture,
        ITestOutputHelper testOutputHelper)
    {
        Fixture = fixture;
        Fixture.SetTestOutputHelper(testOutputHelper);
    }

    private B2BApiAppFixture Fixture { get; }

    public async Task InitializeAsync()
    {
        Fixture.AppHostManager.ClearHostLog();
        Fixture.ServiceBusListenerMock.ResetMessageHandlersAndReceivedMessages();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        Fixture.SetTestOutputHelper(null!);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that:
    /// - a RequestWholesaleServices HTTP request can complete successfully.
    /// - A message is added to the Process Manager service bus topic.
    /// </summary>
    [Fact]
    public async Task Given_RequestWholesaleServices_When_RequestIsReceived_Then_ServiceBusMessageIsSentToProcessManagerTopic()
    {
        // Arrange
        // The following must match with the JSON/XML document content
        var energySupplier = new Actor(
            ActorNumber.Create("5790000701278"),
            ActorRole.EnergySupplier);

        var transactionId = Guid.NewGuid().ToString();
        var idempotencyKey = $"{transactionId}_{energySupplier.ActorNumber.Value}_{energySupplier.ActorRole.Code}";

        // Test steps:
        // => HTTP POST: RequestWholesaleServices
        using var httpRequest = await Fixture.CreateRequestWholesaleServicesHttpRequestAsync(energySupplier, transactionId);
        using var httpResponse = await Fixture.AppHostManager.HttpClient.SendAsync(httpRequest);
        await httpResponse.EnsureSuccessStatusCodeWithLogAsync(Fixture.TestLogger);

        // => Assert service bus message is sent to Process Manager topic
        var verifyServiceBusMessage = await Fixture.ServiceBusListenerMock
            .When(
                msg =>
                {
                    var messageIdMatch = msg.MessageId.Equals(idempotencyKey);
                    var subjectMatch = msg.Subject.Equals(Brs_028.Name);

                    return messageIdMatch && subjectMatch;
                })
            .VerifyOnceAsync();

        var messageReceived = verifyServiceBusMessage.Wait(TimeSpan.FromSeconds(30));
        messageReceived.Should().BeTrue("because a Brs_028 message should be sent to the Process Manager topic");
    }
}
