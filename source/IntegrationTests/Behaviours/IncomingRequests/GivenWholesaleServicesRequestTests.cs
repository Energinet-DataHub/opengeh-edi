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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.IntegrationTests.TestDoubles;
using Energinet.DataHub.EDI.MasterData.Interfaces.Models;
using Energinet.DataHub.Edi.Requests;
using Energinet.DataHub.Edi.Responses;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.NotifyWholesaleServices;
using FluentAssertions;
using FluentAssertions.Execution;
using NodaTime;
using NodaTime.Serialization.Protobuf;
using NodaTime.Text;
using Xunit;
using Xunit.Abstractions;
using ChargeType = Energinet.DataHub.Edi.Requests.ChargeType;
using Period = Energinet.DataHub.Edi.Responses.Period;
using Resolution = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Resolution;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours.IncomingRequests;

[SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments", Justification = "Test class")]
public class GivenWholesaleServicesRequestTests : BehavioursTestBase
{
    public GivenWholesaleServicesRequestTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
    }

    // [Fact]
    // public async Task AndGiven_DelegationInTwoGridAreas_When_WholesaleServicesProcessIsInitialized_Then_WholesaleServiceBusMessageIsCorrect()
    // {
    //     // Arrange
    //     var senderSpy = CreateServiceBusSenderSpy("Fake");
    //     GivenNowIs(2024, 7, 1);
    //     var delegatedByActor = (ActorNumber: ActorNumber.Create("2111111111111"), ActorRole: ActorRole.EnergySupplier);
    //     var delegatedToActor = (ActorNumber: ActorNumber.Create("1111111111111"), ActorRole: ActorRole.Delegated);
    //     GivenAuthenticatedActorIs(delegatedToActor.ActorNumber, delegatedToActor.ActorRole);
    //
    //     await GivenDelegation(
    //         new ActorNumberAndRoleDto(delegatedByActor.ActorNumber, delegatedByActor.ActorRole),
    //         new ActorNumberAndRoleDto(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
    //         "512",
    //         ProcessType.RequestWholesaleResults,
    //         GetNow(),
    //         GetNow().Plus(Duration.FromDays(32)));
    //
    //     await GivenDelegation(
    //         new ActorNumberAndRoleDto(delegatedByActor.ActorNumber, delegatedByActor.ActorRole),
    //         new ActorNumberAndRoleDto(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
    //         "609",
    //         ProcessType.RequestWholesaleResults,
    //         GetNow(),
    //         GetNow().Plus(Duration.FromDays(32)));
    //
    //     await GivenRequestWholesaleServices(
    //         DocumentFormat.Json,
    //         delegatedToActor.ActorNumber.Value,
    //         delegatedByActor.ActorRole.Code,
    //         (2024, 1, 1),
    //         (2024, 2, 1),
    //         null,
    //         delegatedByActor.ActorNumber.Value,
    //         "123564789123564789123564789123564787",
    //         false);
    //
    //     // Act
    //     await WhenWholesaleServicesProcessIsInitialized(senderSpy.Message!);
    //
    //     // Assert
    //     await ThenWholesaleServicesRequestServiceBusMessageIsCorrect(
    //         senderSpy,
    //         gridAreas: new[] { "512", "609" },
    //         requestedForActorNumber: "2111111111111",
    //         requestedForActorRole: "EnergySupplier",
    //         energySupplierId: "2111111111111");
    // }

    [Fact]
    public async Task AndGiven_DelegationInTwoGridAreas_When_DelegatedActorPeeksMessage_Then_NotifyWholesaleServicesDocumentIsCorrect()
    {
        // TODO: Same test, but just for rejected instead
        var documentFormat = DocumentFormat.Json; // TODO: Make input parameter
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
        var delegatedByActor = (ActorNumber: ActorNumber.Create("1111111111111"), ActorRole: ActorRole.EnergySupplier);
        var delegatedToActor = (ActorNumber: ActorNumber.Create("2222222222222"), ActorRole: ActorRole.Delegated);

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(delegatedToActor.ActorNumber, delegatedToActor.ActorRole);

        await GivenDelegation(
            new ActorNumberAndRoleDto(delegatedByActor.ActorNumber, delegatedByActor.ActorRole),
            new ActorNumberAndRoleDto(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
            "512",
            ProcessType.RequestWholesaleResults,
            GetNow(),
            GetNow().Plus(Duration.FromDays(32)));

        await GivenDelegation(
            new ActorNumberAndRoleDto(delegatedByActor.ActorNumber, delegatedByActor.ActorRole),
            new ActorNumberAndRoleDto(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
            "609",
            ProcessType.RequestWholesaleResults,
            GetNow(),
            GetNow().Plus(Duration.FromDays(32)));

        await GivenRequestWholesaleServices(
            documentFormat,
            delegatedToActor.ActorNumber.Value,
            delegatedByActor.ActorRole.Code,
            (2024, 1, 1),
            (2024, 1, 31),
            // null,
            "609",
            delegatedByActor.ActorNumber.Value,
            "5799999933444",
            "25361478",
            BuildingBlocks.Domain.Models.ChargeType.Tariff.Code,
            "123564789123564789123564789123564787",
            false);

        // Act
        await WhenWholesaleServicesProcessIsInitialized(senderSpy.Message!);

        // Assert
        var message = await ThenWholesaleServicesRequestServiceBusMessageIsCorrect(
            senderSpy,
            // gridAreas: new[] { "512", "609" },
            gridAreas: new[] { "609" },
            requestedForActorNumber: "1111111111111",
            requestedForActorRole: "EnergySupplier",
            energySupplierId: "1111111111111");

        // TODO: Assert correct process is created

        /*
         *  --- PART 2: Receive data from Wholesale and create RSM document ---
         */

        // Arrange
        var wholesaleServicesRequestAcceptedMessage = GenerateWholesaleServicesRequestAcceptedMessage(message.WholesaleServicesRequest);
        await GivenWholesaleServicesRequestAcceptedIsReceived(message.ProcessId, wholesaleServicesRequestAcceptedMessage);

        // Act
        var peekResult = await WhenActorPeeksMessage(
            delegatedToActor.ActorNumber,
            delegatedToActor.ActorRole,
            documentFormat);

        await ThenNotifyWholesaleServicesDocumentIsCorrect(
            peekResult.Bundle,
            documentFormat,
            document => document
                // -- Assert header values --
                .MessageIdExists()
                // Assert businessSector.type? (23)
                .HasTimestamp("2024-07-01T14:57:09Z") // 2024, 7, 1, 14, 57, 09
                .HasBusinessReason(BusinessReason.WholesaleFixing, CodeListType.EbixDenmark)
                .HasReceiverId(ActorNumber.Create("2222222222222"))
                .HasReceiverRole(ActorRole.EnergySupplier, CodeListType.Ebix)
                .HasSenderId(ActorNumber.Create("5790001330552"), "A10") // Sender is DataHub
                .HasSenderRole(ActorRole.MeteredDataAdministrator)
                // Assert type? (E31)
                // -- Assert series values --
                .TransactionIdExists()
                .HasChargeTypeOwner(ActorNumber.Create("5799999933444"), "A10")
                .HasChargeCode("25361478")
                .HasChargeType(BuildingBlocks.Domain.Models.ChargeType.Tariff)
                .HasCurrency(Currency.DanishCrowns)
                .HasEnergySupplierNumber(ActorNumber.Create("1111111111111"), "A10")
                .HasSettlementMethod(SettlementMethod.Flex)
                .HasMeteringPointType(MeteringPointType.Consumption)
                .HasGridAreaCode("609", "NDK")
                .HasOriginalTransactionIdReference("123564789123564789123564789123564787")
                .HasPriceMeasurementUnit(MeasurementUnit.Kwh)
                .HasProductCode("5790001330590") // Example says "8716867000030", but document writes as "5790001330590"?
                .HasQuantityMeasurementUnit(MeasurementUnit.Kwh)
                .SettlementVersionDoesNotExist()
                .HasCalculationVersion(GetNow().ToUnixTimeTicks())
                .HasResolution(Resolution.Hourly)
                .HasPeriod(
                    new BuildingBlocks.Domain.Models.Period(
                        CreateDateInstant(2024, 1, 1),
                        CreateDateInstant(2024, 1, 31)))
                .HasPoints(wholesaleServicesRequestAcceptedMessage.Series.First().TimeSeriesPoints));
    }

    private WholesaleServicesRequestAccepted GenerateWholesaleServicesRequestAcceptedMessage(WholesaleServicesRequest request)
    {
        var gridAreas = request.GridAreaCodes.ToList();
        if (gridAreas.Count == 0)
            gridAreas.AddRange(new List<string> { "804", "917" });

        var chargeTypes = request.ChargeTypes;
        if (chargeTypes.Count == 0)
            chargeTypes.Add(new ChargeType { ChargeCode = "12345678", ChargeType_ = DataHubNames.ChargeType.Tariff });

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
                    MeteringPointType = WholesaleServicesRequestSeries.Types.MeteringPointType.Consumption,
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
