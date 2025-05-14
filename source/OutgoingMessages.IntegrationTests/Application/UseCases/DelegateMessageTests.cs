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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.MasterData.Interfaces.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Application.UseCases;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.ActorMessagesQueues;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.IntegrationTests.Fixtures;
using NodaTime;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.OutgoingMessages.IntegrationTests.Application.UseCases;

public class DelegateMessageTests : OutgoingMessagesTestBase
{
    private const string GridAreaCode = "001";
    private readonly Actor _delegatedTo;
    private readonly Actor _delegatedBy;
    private readonly DelegateMessage _sut;
    private readonly Instant _now;

    public DelegateMessageTests(
        OutgoingMessagesTestFixture outgoingMessagesTestFixture,
        ITestOutputHelper testOutputHelper)
        : base(outgoingMessagesTestFixture, testOutputHelper)
    {
        _delegatedTo = new Actor(ActorNumber.Create("1111111111111"), actorRole: ActorRole.Delegated);
        _delegatedBy = new Actor(ActorNumber.Create("4444444444444"), actorRole: ActorRole.EnergySupplier);

        _sut = GetService<DelegateMessage>();
        _now = SystemClock.Instance.GetCurrentInstant();
    }

    public static TheoryData<ProcessType> GetAllDelegationProcessTypes => new(
        EnumerationType.GetAll<ProcessType>()
            .Where(x => !NotDelegationProcessTypes().Contains(x))
            .ToArray());

    public static IEnumerable<ProcessType> NotDelegationProcessTypes()
    {
        return new List<ProcessType>
        {
            ProcessType.RequestEnergyResults,
            ProcessType.RequestWholesaleResults,
        };
    }

    [Theory]
    [MemberData(nameof(GetAllDelegationProcessTypes))]
    public async Task Given_DelegationIsSet_When_DelegateAsync_Then_MessageIsDelegated(ProcessType processType)
    {
        var message = CreateOutgoingMessage(_delegatedBy, processType);
        await AddDelegationAsync(_delegatedBy, _delegatedTo, processType);

        var result = await _sut.DelegateAsync(message, CancellationToken.None);

        Assert.Equal(_delegatedBy.ActorNumber, result.DocumentReceiver.Number);
        Assert.Equal(_delegatedBy.ActorRole, result.DocumentReceiver.ActorRole);

        Assert.Equal(_delegatedTo.ActorNumber, result.Receiver.Number);
        Assert.Equal(_delegatedTo.ActorRole, result.Receiver.ActorRole);
    }

    [Theory]
    [MemberData(nameof(GetAllDelegationProcessTypes))]
    public async Task Given_DocumentTypeIsAcknowledgement_When_DelegateAsync_Then_NoMessagesAreDelegated(ProcessType processType)
    {
        var message = CreateOutgoingMessage(_delegatedBy, processType, DocumentType.Acknowledgement);
        await AddDelegationAsync(_delegatedBy, _delegatedTo, processType);

        var result = await _sut.DelegateAsync(message, CancellationToken.None);

        Assert.Equal(_delegatedBy.ActorNumber, result.DocumentReceiver.Number);
        Assert.Equal(_delegatedBy.ActorRole, result.DocumentReceiver.ActorRole);

        Assert.Equal(_delegatedBy.ActorNumber, result.Receiver.Number);
        Assert.Equal(_delegatedBy.ActorRole, result.Receiver.ActorRole);
    }

    private OutgoingMessage CreateOutgoingMessage(
        Actor actor,
        ProcessType processType,
        DocumentType? documentType = null)
    {
        var receiver = Receiver.Create(actor.ActorNumber, actorRole: actor.ActorRole);
        return new OutgoingMessage(
            eventId: EventId.From(Guid.NewGuid()),
            documentType: documentType ?? DocumentType.NotifyAggregatedMeasureData,
            receiver: receiver,
            documentReceiver: receiver,
            processId: Guid.NewGuid(),
            businessReason: BusinessReason.BalanceFixing.Name,
            serializedContent: "dummy",
            createdAt: _now,
            messageCreatedFromProcess: processType,
            relatedToMessageId: null,
            gridAreaCode: GridAreaCode,
            externalId: ExternalId.New(),
            calculationId: null,
            periodStartedAt: null,
            dataCount: 1,
            meteringPointId: null);
    }

    private async Task AddDelegationAsync(
        Actor delegatedBy,
        Actor delegatedTo,
        ProcessType processType,
        int sequenceNumber = 0)
    {
        var masterDataClient = GetService<IMasterDataClient>();
        await masterDataClient.CreateProcessDelegationAsync(
            new ProcessDelegationDto(
                sequenceNumber,
                processType,
                GridAreaCode,
                StartsAt: _now.Minus(Duration.FromDays(5)),
                StopsAt: _now.Plus(Duration.FromDays(5)),
                delegatedBy,
                delegatedTo),
            CancellationToken.None);
    }
}
