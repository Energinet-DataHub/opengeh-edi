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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.ProcessManager;
using Energinet.DataHub.EDI.Process.Interfaces;
using Energinet.DataHub.ProcessManager.Abstractions.Api.Model;
using Energinet.DataHub.ProcessManager.Abstractions.Api.Model.OrchestrationInstance;
using Energinet.DataHub.ProcessManager.Client;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_021.ForwardMeteredData.V1.Model;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_026.V1.Model;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_028.V1.Model;
using FluentAssertions;
using Moq;
using Xunit;
using ChargeType = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.ChargeType;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.IncomingMessages;

public class RequestProcessOrchestrationStarterTests
{
    [Theory]
    [InlineData("2023-05-31T22:00:00Z", "904", "1111111222222", "2222222222222", "PT15M", "D01", "AAA", "D03")]
    [InlineData(null, null, null, null, null, null, null, null)]
    public async Task Given_RequestForWholesaleServices_When_StartRequestWholesaleServicesCalled_Then_ProcessManagerMessageClientIsCalled(
        string? expectedEnd,
        string? expectedGridArea,
        string? expectedEnergySupplierNumber,
        string? expectedChargeOwnerNumber,
        string? resolutionCode,
        string? settlementVersionCode,
        string? expectedChargeId,
        string? chargeTypeCode)
    {
        // Arrange
        // => Setup request input
        var requestedByActor = RequestedByActor.From(ActorNumber.Create("1111111111111"), ActorRole.GridAccessProvider);

        var expectedBusinessReason = BusinessReason.WholesaleFixing;
        var expectedTransactionId = "85f00b2e-cbfa-4b17-86e0-b9004d683f9f";
        var expectedStart = "2023-04-30T22:00:00Z";
        var expectedResolution = resolutionCode is not null
            ? Resolution.FromCode(resolutionCode)
            : null;
        var expectedSettlementVersion = settlementVersionCode is not null
            ? SettlementVersion.FromCode(settlementVersionCode)
            : null;
        var expectedChargeType = chargeTypeCode is not null
            ? ChargeType.FromCode(chargeTypeCode)
            : null;

        var initializeProcessDto = new InitializeWholesaleServicesProcessDto(
            BusinessReason: expectedBusinessReason.Code,
            MessageId: MessageId.Create("9b6184af-2f05-40b9-d783-08dc814df95a").Value,
            Series:
            [
                new InitializeWholesaleServicesSeries(
                    Id: TransactionId.From(expectedTransactionId).Value,
                    StartDateTime: expectedStart,
                    EndDateTime: expectedEnd,
                    RequestedGridAreaCode: expectedGridArea,
                    EnergySupplierId: expectedEnergySupplierNumber,
                    SettlementVersion: expectedSettlementVersion?.Code,
                    Resolution: expectedResolution?.Code,
                    ChargeOwner: expectedChargeOwnerNumber,
                    ChargeTypes: [new InitializeWholesaleServicesChargeType(
                        Id: expectedChargeId,
                        Type: expectedChargeType?.Code)],
                    GridAreas: expectedGridArea is not null ? [expectedGridArea] : [],
                    RequestedByActor: requestedByActor,
                    OriginalActor.From(requestedByActor))
            ]);

        // => Setup Process Manager client and callback
        var processManagerClient = new Mock<IProcessManagerMessageClient>();
        MessageCommand<RequestCalculatedWholesaleServicesInputV1>? actualCommand = null;
        processManagerClient.Setup(
                client => client.StartNewOrchestrationInstanceAsync(
                    It.IsAny<RequestCalculatedWholesaleServicesCommandV1>(),
                    It.IsAny<CancellationToken>()))
            .Callback((MessageCommand<RequestCalculatedWholesaleServicesInputV1> command, CancellationToken token) => actualCommand = command);

        // => Setup authenticated actor
        var expectedActorId = Guid.NewGuid();
        var authenticatedActor = new AuthenticatedActor();
        authenticatedActor.SetAuthenticatedActor(new ActorIdentity(
            ActorNumber.Create("1234567890123"),
            Restriction.None,
            ActorRole.GridAccessProvider,
            expectedActorId));

        var sut = new RequestProcessOrchestrationStarter(
            processManagerClient.Object,
            authenticatedActor);

        // Act
        await sut.StartRequestWholesaleServicesOrchestrationAsync(
            initializeProcessDto,
            CancellationToken.None);

        // Assert
        processManagerClient.Verify(
            client => client.StartNewOrchestrationInstanceAsync(
                It.IsAny<RequestCalculatedWholesaleServicesCommandV1>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        var expectedCommand = new RequestCalculatedWholesaleServicesCommandV1(
            operatingIdentity: new ActorIdentityDto(expectedActorId),
            inputParameter: new RequestCalculatedWholesaleServicesInputV1(
                RequestedForActorNumber: requestedByActor.ActorNumber.Value,
                RequestedForActorRole: requestedByActor.ActorRole.Name,
                BusinessReason: expectedBusinessReason.Name,
                Resolution: expectedResolution?.Name,
                PeriodStart: expectedStart,
                PeriodEnd: expectedEnd,
                EnergySupplierNumber: expectedEnergySupplierNumber,
                ChargeOwnerNumber: expectedChargeOwnerNumber,
                GridAreas: expectedGridArea is not null ? [expectedGridArea] : [],
                SettlementVersion: expectedSettlementVersion?.Name,
                ChargeTypes:
                [
                    new RequestCalculatedWholesaleServicesInputV1.ChargeTypeInputV1(
                        ChargeType: expectedChargeType?.Name,
                        ChargeCode: expectedChargeId)
                ]),
            messageId: expectedTransactionId);

        actualCommand.Should()
            .NotBeNull()
            .And.BeEquivalentTo(expectedCommand);
    }

    [Theory]
    [InlineData("2023-05-31T22:00:00Z", "904", "1111111222222", "2222222222222", "E18", "E02", "D02")]
    [InlineData(null, null, null, null, null, null, null)]
    public async Task Given_RequestForAggregatedMeasureDataCValues_When_StartRequestAggregatedMeasureDataCalled_Then_ProcessManagerMessageClientIsCalled(
        string? expectedEnd,
        string? expectedGridArea,
        string? expectedEnergySupplierNumber,
        string? expectedBalanceResponsibleNumber,
        string? meteringPointTypeCode,
        string? settlementMethodCode,
        string? settlementVersionCode)
    {
        // Arrange
        // => Setup request input
        var requestedByActor = RequestedByActor.From(ActorNumber.Create("1111111111111"), ActorRole.EnergySupplier);

        var expectedBusinessReason = BusinessReason.BalanceFixing;
        var expectedTransactionId = "85f00b2e-cbfa-4b17-86e0-b9004d683f9f";
        var expectedStart = "2023-04-30T22:00:00Z";
        var expectedSettlementVersion = settlementVersionCode is not null
            ? SettlementVersion.FromCode(settlementVersionCode)
            : null;
        var expectedMeteringPointType = meteringPointTypeCode is not null
            ? MeteringPointType.FromCode(meteringPointTypeCode)
            : null;
        var expectedSettlementMethod = settlementMethodCode is not null
            ? SettlementMethod.FromCode(settlementMethodCode)
            : null;

        var initializeProcessDto = new InitializeAggregatedMeasureDataProcessDto(
            SenderNumber: requestedByActor.ActorNumber.Value,
            SenderRoleCode: requestedByActor.ActorRole.Code,
            BusinessReason: expectedBusinessReason.Code,
            MessageId: MessageId.Create("9b6184af-2f05-40b9-d783-08dc814df95a").Value,
            Series:
            [
                new InitializeAggregatedMeasureDataProcessSeries(
                    Id: TransactionId.From(expectedTransactionId),
                    MeteringPointType: expectedMeteringPointType?.Code,
                    SettlementMethod: expectedSettlementMethod?.Code,
                    StartDateTime: expectedStart,
                    EndDateTime: expectedEnd,
                    RequestedGridAreaCode: expectedGridArea,
                    EnergySupplierNumber: expectedEnergySupplierNumber,
                    BalanceResponsibleNumber: expectedBalanceResponsibleNumber,
                    SettlementVersion: expectedSettlementVersion?.Code,
                    GridAreas: expectedGridArea is not null ? [expectedGridArea] : [],
                    RequestedByActor: requestedByActor,
                    OriginalActor: OriginalActor.From(requestedByActor))
            ]);

        // => Setup Process Manager client and callback
        var processManagerClient = new Mock<IProcessManagerMessageClient>();
        MessageCommand<RequestCalculatedEnergyTimeSeriesInputV1>? actualCommand = null;
        processManagerClient.Setup(
                client => client.StartNewOrchestrationInstanceAsync(
                    It.IsAny<StartRequestCalculatedEnergyTimeSeriesCommandV1>(),
                    It.IsAny<CancellationToken>()))
            .Callback((MessageCommand<RequestCalculatedEnergyTimeSeriesInputV1> command, CancellationToken token) => actualCommand = command);

        // => Setup authenticated actor
        var expectedActorId = Guid.NewGuid();
        var authenticatedActor = new AuthenticatedActor();
        authenticatedActor.SetAuthenticatedActor(new ActorIdentity(
            ActorNumber.Create("1234567890123"),
            Restriction.None,
            ActorRole.EnergySupplier,
            expectedActorId));

        var sut = new RequestProcessOrchestrationStarter(
            processManagerClient.Object,
            authenticatedActor);

        // Act
        await sut.StartRequestAggregatedMeasureDataOrchestrationAsync(
            initializeProcessDto,
            CancellationToken.None);

        // Assert
        processManagerClient.Verify(
            client => client.StartNewOrchestrationInstanceAsync(
                It.IsAny<StartRequestCalculatedEnergyTimeSeriesCommandV1>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        var expectedCommand = new StartRequestCalculatedEnergyTimeSeriesCommandV1(
            operatingIdentity: new ActorIdentityDto(expectedActorId),
            inputParameter: new RequestCalculatedEnergyTimeSeriesInputV1(
                RequestedForActorNumber: requestedByActor.ActorNumber.Value,
                RequestedForActorRole: requestedByActor.ActorRole.Name,
                BusinessReason: expectedBusinessReason.Name,
                PeriodStart: expectedStart,
                PeriodEnd: expectedEnd,
                EnergySupplierNumber: expectedEnergySupplierNumber,
                BalanceResponsibleNumber: expectedBalanceResponsibleNumber,
                GridAreas: expectedGridArea is not null ? [expectedGridArea] : [],
                MeteringPointType: expectedMeteringPointType?.Name,
                SettlementMethod: expectedSettlementMethod?.Name,
                SettlementVersion: expectedSettlementVersion?.Name),
            messageId: expectedTransactionId);

        actualCommand.Should()
            .NotBeNull()
            .And.BeEquivalentTo(expectedCommand);
    }

    [Theory]
    [InlineData("571313101700011887", "E17", "2023-05-31T22:00:00Z", "8716867000030", "Kwh", "PT1H")]
    [InlineData(null, null, null, null, null, null)]
    public async Task Given_MeteredDataForMeasurementPoint_When_StartMeteredDataForMeasurementCalled_Then_ProcessManagerMessageClientIsCalled(
        string? expectedMeteringPointId,
        string? expectedMeteringPointType,
        string? expectedEndDate,
        string? expectedProductNumber,
        string? expectedMeasureUnit,
        string? expectedResolution)
    {
        // Arrange
        // => Setup input
        var requestedByActor = RequestedByActor.From(ActorNumber.Create("1111111111111"), ActorRole.GridAccessProvider);

        var expectedBusinessReason = BusinessReason.PeriodicMetering;
        var expectedTransactionId = MessageId.Create("9b6184bf-2f05-40b9-d783-08dc814df95a").Value;
        var expectedStart = "2023-04-30T22:00:00Z";
        var expectedRegistrationDateFrom = "2023-04-30T22:00:00Z";
        var expectedPosition = "1";
        var expectedEnergyQuantity = "A03";
        var expectedQuantityQuality = "1001";

        var meteringPointType = expectedMeteringPointType is not null
            ? MeteringPointType.FromCode(expectedMeteringPointType)
            : null;
        var productUnitType = expectedMeasureUnit is not null
            ? MeasurementUnit.FromCode(expectedMeasureUnit)
            : null;
        var resolution = expectedResolution is not null
            ? Resolution.FromCode(expectedResolution)
            : null;

        var initializeProcessDto = new InitializeMeteredDataForMeasurementPointMessageProcessDto(
            MessageId: expectedTransactionId,
            MessageType: "E66",
            CreatedAt: expectedRegistrationDateFrom,
            BusinessReason: expectedBusinessReason.Code,
            BusinessType: "23",
            Series:
            [
                new InitializeMeteredDataForMeasurementPointMessageSeries(
                    TransactionId: expectedTransactionId,
                    Resolution: resolution?.Code,
                    StartDateTime: expectedStart,
                    EndDateTime: expectedEndDate,
                    ProductNumber: expectedProductNumber,
                    ProductUnitType: productUnitType?.Code,
                    MeteringPointType: meteringPointType?.Code,
                    MeteringPointLocationId: expectedMeteringPointId,
                    RegisteredAt: expectedRegistrationDateFrom,
                    DelegatedGridAreaCodes: null,
                    RequestedByActor: requestedByActor,
                    EnergyObservations:
                    [
                        new InitializeEnergyObservation(
                            Position: expectedPosition,
                            EnergyQuantity: expectedEnergyQuantity,
                            QuantityQuality: expectedQuantityQuality)
                    ])
            ]);

        // => Setup Process Manager client and callback
        var processManagerClient = new Mock<IProcessManagerMessageClient>();
        MessageCommand<MeteredDataForMeasurementPointMessageInputV1>? actualCommand = null;
        processManagerClient.Setup(
                client => client.StartNewOrchestrationInstanceAsync(
                    It.IsAny<StartForwardMeteredDataCommandV1>(),
                    It.IsAny<CancellationToken>()))
            .Callback((MessageCommand<MeteredDataForMeasurementPointMessageInputV1> command, CancellationToken token) => actualCommand = command);

        // => Setup authenticated actor
        var expectedActorId = Guid.NewGuid();
        var authenticatedActor = new AuthenticatedActor();
        authenticatedActor.SetAuthenticatedActor(new ActorIdentity(
            ActorNumber.Create("1234567890123"),
            Restriction.None,
            ActorRole.GridAccessProvider,
            expectedActorId));

        var sut = new MeteredDataOrchestrationStarter(
            processManagerClient.Object,
            authenticatedActor);

        // Act
        await sut.StartForwardMeteredDataForMeasurementPointOrchestrationAsync(
            initializeProcessDto,
            CancellationToken.None);

        // Assert
        processManagerClient.Verify(
            client => client.StartNewOrchestrationInstanceAsync(
                It.IsAny<StartForwardMeteredDataCommandV1>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        var expectedCommand = new StartForwardMeteredDataCommandV1(
            operatingIdentity: new ActorIdentityDto(expectedActorId),
            inputParameter: new MeteredDataForMeasurementPointMessageInputV1(
                AuthenticatedActorId: expectedActorId,
                TransactionId: expectedTransactionId,
                MeteringPointId: expectedMeteringPointId,
                MeteringPointType: meteringPointType?.Name,
                ProductNumber: expectedProductNumber,
                MeasureUnit: expectedMeasureUnit,
                RegistrationDateTime: expectedRegistrationDateFrom,
                Resolution: resolution?.Name,
                StartDateTime: expectedStart,
                EndDateTime: expectedEndDate,
                GridAccessProviderNumber: requestedByActor.ActorNumber.Value,
                DelegatedGridAreaCodes: null,
                EnergyObservations:
                [
                    new ProcessManager.Orchestrations.Abstractions.Processes.BRS_021.ForwardMeteredData.V1.Model.EnergyObservation(
                        Position: expectedPosition,
                        EnergyQuantity: expectedEnergyQuantity,
                        QuantityQuality: expectedQuantityQuality)
                ]),
            messageId: expectedTransactionId);

        actualCommand.Should()
            .NotBeNull()
            .And.BeEquivalentTo(expectedCommand);
    }
}
