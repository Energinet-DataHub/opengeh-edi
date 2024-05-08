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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.MessageBus;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using Energinet.DataHub.EDI.IntegrationTests.EventBuilders;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.IntegrationTests.TestDoubles;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.MasterData.Interfaces.Models;
using Energinet.DataHub.EDI.Process.Interfaces;
using FluentAssertions;
using FluentAssertions.Execution;
using NodaTime;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.IncomingMessages;

public class WhenIncomingMessagesIsReceivedWithDelegationTests : TestBase
{
    private readonly SystemDateTimeProviderStub _dateTimeProvider;
    private readonly IIncomingMessageClient _incomingMessagesRequest;

    private readonly Actor _originalActor = new(ActorNumber.Create("1111111111111"), ActorRole.EnergySupplier);
    private readonly Actor _delegatedTo = new(ActorNumber.Create("2222222222222"), ActorRole.Delegated);
    private readonly ServiceBusSenderSpy _senderSpy;
    private readonly ServiceBusSenderFactoryStub _serviceBusClientSenderFactory;
    private readonly AuthenticatedActor _authenticatedActor;

    public WhenIncomingMessagesIsReceivedWithDelegationTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
        _serviceBusClientSenderFactory = (ServiceBusSenderFactoryStub)GetService<IServiceBusSenderFactory>();
        _senderSpy = new ServiceBusSenderSpy("Fake");
        _serviceBusClientSenderFactory.AddSenderSpy(_senderSpy);
        _incomingMessagesRequest = GetService<IIncomingMessageClient>();
        _dateTimeProvider = (SystemDateTimeProviderStub)GetService<ISystemDateTimeProvider>();
        _authenticatedActor = GetService<AuthenticatedActor>();
    }

    [Fact]
    public async Task Receive_message_from_delegated()
    {
        // Arrange
        var now = Instant.FromUtc(2024, 05, 07, 13, 37);
        _dateTimeProvider.SetNow(now);
        var gridAreaCode = "512";
        var documentFormat = DocumentFormat.Json;
        _authenticatedActor.SetAuthenticatedActor(new ActorIdentity(_delegatedTo.ActorNumber, Restriction.Owned, _delegatedTo.ActorRole));

        var messageStream = RequestAggregatedMeasureDataRequestBuilder.CreateIncomingMessage(
            DocumentFormat.Json,
            _delegatedTo.ActorNumber,
            _originalActor.ActorRole,
            null,
            null,
            Instant.FromUtc(2024, 01, 01, 0, 0),
            Instant.FromUtc(2024, 01, 31, 0, 0),
            _originalActor.ActorNumber,
            null,
            new List<(string? GridArea, string TransactionId)> { (gridAreaCode, "555555555555555555555555555555555555"), });

        await AddDelegationAsync(
            _originalActor,
            _delegatedTo,
            gridAreaCode,
            ProcessType.RequestEnergyResults,
            startsAt: now,
            stopsAt: now.Plus(Duration.FromSeconds(1)));

        // Act
        var response = await _incomingMessagesRequest.RegisterAndSendAsync(
            messageStream,
            documentFormat,
            IncomingDocumentType.RequestAggregatedMeasureData,
            documentFormat,
            CancellationToken.None);

        // Assert
        using (new AssertionScope())
        {
            response.IsErrorResponse.Should().BeFalse();
            response.MessageBody.Should().BeNullOrEmpty();

            _senderSpy.LatestMessage.Should().NotBeNull();
        }

        using (new AssertionScope())
        {
            var message = _senderSpy.LatestMessage!.Body.ToObjectFromJson<InitializeAggregatedMeasureDataProcessDto>();
            var series = message.Series.Should().ContainSingle().Subject;
            series.RequestedByActor.ActorRole.Should().Be(_delegatedTo.ActorRole);
            series.RequestedByActor.ActorNumber.Should().Be(_delegatedTo.ActorNumber);
            series.OriginalActor.ActorRole.Should().Be(_originalActor.ActorRole);
            series.OriginalActor.ActorNumber.Should().Be(_originalActor.ActorNumber);
            series.EnergySupplierNumber.Should().Be(_originalActor.ActorNumber.Value);
            series.RequestedGridAreaCode.Should().Be(gridAreaCode);
            series.GridAreas.Should().Equal(gridAreaCode);
        }
    }

    [Fact]
    public async Task Receive_message_from_delegated_when_delegation_has_stopped()
    {
        // Arrange
        var now = Instant.FromUtc(2024, 05, 07, 13, 37);
        _dateTimeProvider.SetNow(now);
        var gridAreaCode = "512";
        var documentFormat = DocumentFormat.Json;
        _authenticatedActor.SetAuthenticatedActor(new ActorIdentity(_delegatedTo.ActorNumber, Restriction.Owned, _delegatedTo.ActorRole));

        var messageStream = RequestAggregatedMeasureDataRequestBuilder.CreateIncomingMessage(
            DocumentFormat.Json,
            _delegatedTo.ActorNumber,
            _originalActor.ActorRole,
            null,
            null,
            Instant.FromUtc(2024, 01, 01, 0, 0),
            Instant.FromUtc(2024, 01, 31, 0, 0),
            _originalActor.ActorNumber,
            null,
            new List<(string? GridArea, string TransactionId)> { (gridAreaCode, "555555555555555555555555555555555555"), },
            true);

        await AddDelegationAsync(
            _originalActor,
            _delegatedTo,
            gridAreaCode,
            ProcessType.RequestEnergyResults,
            startsAt: now.Minus(Duration.FromMinutes(10)),
            stopsAt: now.Plus(Duration.FromMinutes(10)),
            sequenceNumber: 1);

        // Cancel a delegation by adding a newer (higher sequence number) delegation to same receiver, with startsAt == stopsAt
        await AddDelegationAsync(
            _originalActor,
            _delegatedTo,
            gridAreaCode,
            ProcessType.RequestEnergyResults,
            startsAt: now,
            stopsAt: now,
            sequenceNumber: 2);

        // Act
        var response = await _incomingMessagesRequest.RegisterAndSendAsync(
            messageStream,
            documentFormat,
            IncomingDocumentType.RequestAggregatedMeasureData,
            documentFormat,
            CancellationToken.None);

        // Assert
        using var scope = new AssertionScope();
        response.IsErrorResponse.Should().BeTrue();
        response.MessageBody.Should().Contain("The authenticated user does not hold the required role");
        _senderSpy.LatestMessage.Should().BeNull();
    }

    protected override void Dispose(bool disposing)
    {
        _serviceBusClientSenderFactory.Dispose();
        _senderSpy.Dispose();
        base.Dispose(disposing);
    }

    private async Task AddDelegationAsync(
        Actor delegatedBy,
        Actor delegatedTo,
        string gridAreaCode,
        ProcessType processType,
        Instant? startsAt = null,
        Instant? stopsAt = null,
        int sequenceNumber = 0)
    {
        var masterDataClient = GetService<IMasterDataClient>();
        await masterDataClient.CreateProcessDelegationAsync(
            new ProcessDelegationDto(
                sequenceNumber,
                processType ?? ProcessType.RequestEnergyResults,
                gridAreaCode,
                startsAt ?? SystemClock.Instance.GetCurrentInstant().Minus(Duration.FromDays(5)),
                stopsAt ?? SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromDays(5)),
                delegatedBy,
                delegatedTo),
            CancellationToken.None);
    }
}
