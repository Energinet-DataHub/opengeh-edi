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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.IntegrationTests.TestDoubles;
using Energinet.DataHub.EDI.MasterData.Interfaces.Models;
using Energinet.DataHub.Edi.Requests;
using Energinet.DataHub.Edi.Responses;
using FluentAssertions;
using FluentAssertions.Execution;
using NodaTime;
using NodaTime.Serialization.Protobuf;
using NodaTime.Text;
using Xunit;
using ChargeType = Energinet.DataHub.Edi.Requests.ChargeType;
using Period = Energinet.DataHub.Edi.Responses.Period;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours.IncomingRequests;

[SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments", Justification = "Test class")]
public class GivenWholesaleServicesRequestTests : BehavioursTestBase
{
    public GivenWholesaleServicesRequestTests(IntegrationTestFixture integrationTestFixture)
        : base(integrationTestFixture)
    {
    }

    [Fact]
    public async Task
        AndGiven_DelegationInTwoGridAreas_When_WholesaleServicesProcessIsInitialized_Then_WholesaleServiceBusMessageIsCorrect()
    {
        // Arrange
        var senderSpy = CreateServiceBusSenderSpy("Fake");
        GivenNowIs(2024, 7, 1);
        var delegatedByActor = (ActorNumber: ActorNumber.Create("2111111111111"), ActorRole: ActorRole.EnergySupplier);
        var delegatedToActor = (ActorNumber: ActorNumber.Create("1111111111111"), ActorRole: ActorRole.Delegated);
        GivenAuthenticatedActorIs(delegatedToActor.ActorNumber, delegatedToActor.ActorRole);

        await GivenDelegationAsync(
            new ActorNumberAndRoleDto(delegatedByActor.ActorNumber, delegatedByActor.ActorRole),
            new ActorNumberAndRoleDto(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
            "512",
            ProcessType.RequestWholesaleResults,
            GetNow(),
            GetNow().Plus(Duration.FromDays(32)));

        await GivenDelegationAsync(
            new ActorNumberAndRoleDto(delegatedByActor.ActorNumber, delegatedByActor.ActorRole),
            new ActorNumberAndRoleDto(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
            "609",
            ProcessType.RequestWholesaleResults,
            GetNow(),
            GetNow().Plus(Duration.FromDays(32)));

        await GivenRequestWholesaleServicesAsync(
            DocumentFormat.Json,
            delegatedToActor.ActorNumber.Value,
            delegatedByActor.ActorRole.Code,
            (2024, 1, 1),
            (2024, 2, 1),
            null,
            delegatedByActor.ActorNumber.Value,
            "123564789123564789123564789123564787");

        // Act
        await WhenWholesaleServicesProcessIsInitializedAsync(senderSpy.Message!);

        // Assert
        await ThenWholesaleServicesRequestServiceBusMessageIsCorrect(
            senderSpy,
            gridAreas: new[] { "512", "609" },
            requestedForActorNumber: "2111111111111",
            requestedForActorRole: "EnergySupplier",
            energySupplierId: "2111111111111");
    }

    [Fact]
    public async Task
        AndGiven_DelegationInTwoGridAreas_When_DelegatedEnergySupplierPeeksMessage_Then_NotifyWholesaleServicesDocumentIsCorrect()
    {
        /*
         * A request is a test with 2 parts:
         *  1. Send a request to the system (incoming message)
         *  2. Receive data from Wholesale and create RSM document (outgoing message)
         */

        /*
         *  --- PART 1: Receive request, create process and send message to Wholesale ---
         */

        // Arrange
        var senderSpy = CreateServiceBusSenderSpy("Fake");
        GivenNowIs(2024, 7, 1);
        var delegatedByActor = (ActorNumber: ActorNumber.Create("2111111111111"), ActorRole: ActorRole.EnergySupplier);
        var delegatedToActor = (ActorNumber: ActorNumber.Create("1111111111111"), ActorRole: ActorRole.Delegated);
        GivenAuthenticatedActorIs(delegatedToActor.ActorNumber, delegatedToActor.ActorRole);

        await GivenDelegationAsync(
            new ActorNumberAndRoleDto(delegatedByActor.ActorNumber, delegatedByActor.ActorRole),
            new ActorNumberAndRoleDto(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
            "512",
            ProcessType.RequestWholesaleResults,
            GetNow(),
            GetNow().Plus(Duration.FromDays(32)));

        await GivenDelegationAsync(
            new ActorNumberAndRoleDto(delegatedByActor.ActorNumber, delegatedByActor.ActorRole),
            new ActorNumberAndRoleDto(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
            "609",
            ProcessType.RequestWholesaleResults,
            GetNow(),
            GetNow().Plus(Duration.FromDays(32)));

        await GivenRequestWholesaleServicesAsync(
            DocumentFormat.Json,
            delegatedToActor.ActorNumber.Value,
            delegatedByActor.ActorRole.Code,
            (2024, 1, 1),
            (2024, 2, 1),
            null,
            delegatedByActor.ActorNumber.Value,
            "123564789123564789123564789123564787");

        // Act
        await WhenWholesaleServicesProcessIsInitializedAsync(senderSpy.Message!);

        // Assert
        var message = await ThenWholesaleServicesRequestServiceBusMessageIsCorrect(
            senderSpy,
            gridAreas: new[] { "512", "609" },
            requestedForActorNumber: "2111111111111",
            requestedForActorRole: "EnergySupplier",
            energySupplierId: "2111111111111");

        // TODO: Assert correct process is created

        /*
         *  --- PART 2: Receive data from Wholesale and create RSM document ---
         */

        // Arrange
        var wholesaleServicesRequestAcceptedMessage = CreateWholesaleServicesRequestAcceptedMessage(message.WholesaleServicesRequest);
        await GivenWholesaleServicesRequestAcceptedIsReceived(message.ProcessId, wholesaleServicesRequestAcceptedMessage);

        // Act

        // Assert
    }

    private WholesaleServicesRequestAccepted CreateWholesaleServicesRequestAcceptedMessage(WholesaleServicesRequest request)
    {
        var gridAreas = request.GridAreaCodes.ToList();
        if (gridAreas.Count == 0)
            gridAreas.AddRange(new List<string> { "804", "917" });

        var chargeTypes = request.ChargeTypes;
        if (chargeTypes.Count == 0)
            chargeTypes.Add(new ChargeType { ChargeCode = "25361478", ChargeType_ = DataHubNames.ChargeType.Tariff });

        var series = gridAreas.SelectMany(
            ga => chargeTypes.Select(ct =>
            {
                var resolution = request.Resolution == DataHubNames.Resolution.Monthly
                    ? WholesaleServicesRequestSeries.Types.Resolution.Monthly
                    : WholesaleServicesRequestSeries.Types.Resolution.Hour;

                var points = new List<WholesaleServicesRequestSeries.Types.Point>();
                var periodStart = InstantPattern.General.Parse(request.PeriodStart).Value;
                var periodEnd = InstantPattern.General.Parse(request.PeriodEnd).Value;

                if (resolution == WholesaleServicesRequestSeries.Types.Resolution.Monthly)
                {
                    points.Add(CreatePoint(periodEnd, quantityFactor: 30 * 24));
                }
                else
                {
                    var resolutionDuration = resolution switch
                    {
                        WholesaleServicesRequestSeries.Types.Resolution.Day => Duration.FromHours(24),
                        WholesaleServicesRequestSeries.Types.Resolution.Hour => Duration.FromHours(1),
                        _ => throw new NotImplementedException($"Unsupported resolution in request: {resolution.ToString()}"),
                    };

                    var currentTime = periodStart;
                    while (currentTime < periodEnd)
                    {
                        points.Add(CreatePoint(currentTime));
                        currentTime = currentTime.Plus(resolutionDuration);
                    }
                }

                var series = new WholesaleServicesRequestSeries()
                {
                    Currency = WholesaleServicesRequestSeries.Types.Currency.Dkk,
                    Period = new Period
                    {
                        StartOfPeriod = periodStart.ToTimestamp(),
                        EndOfPeriod = periodEnd.ToTimestamp(),
                    },
                    Resolution = resolution,
                    CalculationType = request.BusinessReason == DataHubNames.BusinessReason.WholesaleFixing
                        ? WholesaleServicesRequestSeries.Types.CalculationType.WholesaleFixing
                        : throw new NotImplementedException("Builder only supports WholesaleFixing, not corrections"),
                    ChargeCode = ct.ChargeCode,
                    ChargeType =
                        Enum.TryParse<WholesaleServicesRequestSeries.Types.ChargeType>(ct.ChargeType_, out var result)
                            ? result
                            : throw new NotImplementedException("Unsupported chargetype in request"),
                    ChargeOwnerId = request.HasChargeOwnerId ? request.ChargeOwnerId : "5799999933444",
                    GridArea = ga,
                    QuantityUnit = WholesaleServicesRequestSeries.Types.QuantityUnit.Kwh,
                    SettlementMethod = WholesaleServicesRequestSeries.Types.SettlementMethod.Flex,
                    EnergySupplierId = request.EnergySupplierId,
                    MeteringPointType = WholesaleServicesRequestSeries.Types.MeteringPointType.Production,
                    CalculationResultVersion = GetNow().ToUnixTimeTicks(),
                };

                series.TimeSeriesPoints.AddRange(points);

                return series;
            }));

        var requestAcceptedMessage = new WholesaleServicesRequestAccepted();
        requestAcceptedMessage.Series.AddRange(series);

        return requestAcceptedMessage;
    }

    [SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "Random not used for security")]
    private WholesaleServicesRequestSeries.Types.Point CreatePoint(Instant currentTime, int quantityFactor = 1)
    {
        // Create random price between 0.99 and 5.99
        var price = new DecimalValue { Units = Random.Shared.Next(0, 4), Nanos = Random.Shared.Next(1, 99) };

        // Create random quantity between 1.00 and 999.99 (multiplied a factor used by by monthly resolution)
        var quantity = new DecimalValue { Units = Random.Shared.Next(1, 999) * quantityFactor, Nanos = Random.Shared.Next(0, 99) };

        // Calculate the total amount (price * quantity)
        var totalAmount = price.ToDecimal() * quantity.ToDecimal();

        return new WholesaleServicesRequestSeries.Types.Point
        {
            Time = currentTime.ToTimestamp(),
            Price = price,
            Quantity = price,
            Amount = DecimalValue.FromDecimal(totalAmount),
            QuantityQualities = { QuantityQuality.Calculated },
        };
    }

    private Task GivenWholesaleServicesRequestAcceptedIsReceived(Guid processId, WholesaleServicesRequestAccepted wholesaleServicesRequestAcceptedMessage)
    {
        return HavingReceivedInboxEventAsync(
            eventType: nameof(WholesaleServicesRequestAccepted),
            eventPayload: wholesaleServicesRequestAcceptedMessage,
            processId: processId);
    }

    private Task<(WholesaleServicesRequest WholesaleServicesRequest, Guid ProcessId)> ThenWholesaleServicesRequestServiceBusMessageIsCorrect(
        ServiceBusSenderSpy senderSpy,
        IReadOnlyCollection<string> gridAreas,
        string requestedForActorNumber,
        string requestedForActorRole,
        string energySupplierId)
    {
        using (new AssertionScope())
        {
            senderSpy.MessageSent.Should().BeTrue();
            senderSpy.Message.Should().NotBeNull();
        }

        var serviceBusMessage = senderSpy.Message!;
        Guid processId;
        using (new AssertionScope())
        {
            serviceBusMessage.Subject.Should().Be(nameof(WholesaleServicesRequest));
            serviceBusMessage.Body.Should().NotBeNull();
            serviceBusMessage.ApplicationProperties.TryGetValue("ReferenceId", out var referenceId);
            referenceId.Should().NotBeNull();
            Guid.TryParse(referenceId!.ToString()!, out processId).Should().BeTrue();
        }

        var wholesaleServicesRequestMessage = WholesaleServicesRequest.Parser.ParseFrom(serviceBusMessage.Body);
        wholesaleServicesRequestMessage.Should().NotBeNull();

        using var assertionScope = new AssertionScope();
        wholesaleServicesRequestMessage.GridAreaCodes.Should().BeEquivalentTo(gridAreas);
        wholesaleServicesRequestMessage.RequestedForActorNumber.Should().Be(requestedForActorNumber);
        wholesaleServicesRequestMessage.RequestedForActorRole.Should().Be(requestedForActorRole);
        wholesaleServicesRequestMessage.EnergySupplierId.Should().Be(energySupplierId);

        return Task.FromResult((wholesaleServicesRequestMessage, processId));
    }
}
