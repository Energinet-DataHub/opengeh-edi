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

using System.Diagnostics.CodeAnalysis;
using Azure.Messaging.ServiceBus;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.Edi.Requests;
using Energinet.DataHub.Edi.Responses;
using Energinet.DataHub.ProcessManager.Abstractions.Contracts;
using Energinet.DataHub.ProcessManager.Components.Abstractions.BusinessValidation;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_026_028.BRS_026;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_026_028.BRS_026.V1.Model;
using Energinet.DataHub.ProcessManager.Shared.Extensions;
using NodaTime;
using NodaTime.Serialization.Protobuf;
using NodaTime.Text;
using Duration = NodaTime.Duration;
using Period = Energinet.DataHub.Edi.Responses.Period;
using PMActorNumber = Energinet.DataHub.ProcessManager.Abstractions.Core.ValueObjects.ActorNumber;
using PMActorRole = Energinet.DataHub.ProcessManager.Abstractions.Core.ValueObjects.ActorRole;
using PMBusinessReason = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects.BusinessReason;
using PMMeteringPointType = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects.MeteringPointType;
using PMSettlementMethod = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects.SettlementMethod;
using PMSettlementVersion = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects.SettlementVersion;
using PMTypes = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects;
using Resolution = Energinet.DataHub.Edi.Responses.Resolution;
using SettlementVersion = Energinet.DataHub.Edi.Responses.SettlementVersion;

namespace Energinet.DataHub.EDI.IntegrationTests.EventBuilders;

[SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "Random not used for security")]
internal static class AggregatedTimeSeriesResponseEventBuilder
{
    public static AggregatedTimeSeriesRequestAccepted GenerateAcceptedFrom(
        AggregatedTimeSeriesRequest request, Instant now, IReadOnlyCollection<string>? defaultGridAreas = null)
    {
        if (request.GridAreaCodes.Count == 0 && defaultGridAreas == null)
            throw new ArgumentNullException(nameof(defaultGridAreas), "defaultGridAreas must be set when request has no GridAreaCodes");

        var gridAreas = request.GridAreaCodes.ToList();
        if (gridAreas.Count == 0)
            gridAreas.AddRange(defaultGridAreas!);

        var series = gridAreas
            .Select(gridArea =>
            {
                var periodStart = InstantPattern.General.Parse(request.Period.Start).Value;
                var periodEnd = InstantPattern.General.Parse(request.Period.End).Value;

                var timeSeriesType = GetTimeSeriesType(
                    request.HasSettlementMethod ? request.SettlementMethod : null,
                    !string.IsNullOrEmpty(request.MeteringPointType) ? request.MeteringPointType : null);
                var resolution = Resolution.Pt1H;
                var points = CreatePoints(resolution, periodStart, periodEnd);

                var series = new Series
                {
                    GridArea = gridArea,
                    QuantityUnit = QuantityUnit.Kwh,
                    TimeSeriesType = timeSeriesType,
                    Resolution = resolution,
                    CalculationResultVersion = now.ToUnixTimeTicks(),
                    Period = new Period
                    {
                        StartOfPeriod = periodStart.ToTimestamp(),
                        EndOfPeriod = periodEnd.ToTimestamp(),
                    },
                    TimeSeriesPoints = { points },
                };

                if (request.BusinessReason == BusinessReason.Correction.Name)
                {
                    series.SettlementVersion = request.SettlementVersion switch
                    {
                        var sm when sm == BuildingBlocks.Domain.Models.SettlementVersion.FirstCorrection.Name => SettlementVersion.FirstCorrection,
                        var sm when sm == BuildingBlocks.Domain.Models.SettlementVersion.SecondCorrection.Name => SettlementVersion.SecondCorrection,
                        var sm when sm == BuildingBlocks.Domain.Models.SettlementVersion.ThirdCorrection.Name => SettlementVersion.ThirdCorrection,
                        _ => SettlementVersion.ThirdCorrection,
                    };
                }

                return series;
            });

        var acceptedResponse = new AggregatedTimeSeriesRequestAccepted
        {
            Series = { series },
        };

        return acceptedResponse;
    }

    public static ServiceBusMessage GenerateAcceptedFrom(
        RequestCalculatedEnergyTimeSeriesInputV1 requestCalculatedEnergyTimeSeriesInput,
        IReadOnlyCollection<string>? gridAreas = null,
        string? defaultChargeOwnerId = null,
        string? defaultEnergySupplierId = null)
    {
        var periodEnd = requestCalculatedEnergyTimeSeriesInput.PeriodEnd != null ?
            InstantPattern.General.Parse(requestCalculatedEnergyTimeSeriesInput.PeriodEnd).Value.ToDateTimeOffset()
            : throw new ArgumentNullException(nameof(requestCalculatedEnergyTimeSeriesInput.PeriodEnd), "PeriodEnd must be set");
        var energySupplierNumber = requestCalculatedEnergyTimeSeriesInput.EnergySupplierNumber != null
            ? PMActorNumber.Create(requestCalculatedEnergyTimeSeriesInput.EnergySupplierNumber)
            : defaultEnergySupplierId != null ? PMActorNumber.Create(defaultEnergySupplierId) : null;
        var balanceResponsibleNumber = requestCalculatedEnergyTimeSeriesInput.BalanceResponsibleNumber != null
            ? PMActorNumber.Create(requestCalculatedEnergyTimeSeriesInput.BalanceResponsibleNumber)
            : defaultChargeOwnerId != null ? PMActorNumber.Create(defaultChargeOwnerId) : null;
        var settlementVersion = requestCalculatedEnergyTimeSeriesInput.SettlementVersion != null
            ? PMSettlementVersion.FromName(requestCalculatedEnergyTimeSeriesInput.SettlementVersion)
            : null;
        var meteringPointType = requestCalculatedEnergyTimeSeriesInput.MeteringPointType != null
            ? PMMeteringPointType.FromName(requestCalculatedEnergyTimeSeriesInput.MeteringPointType)
            : null;
        var settlementMethod = requestCalculatedEnergyTimeSeriesInput.SettlementMethod != null
            ? PMSettlementMethod.FromName(requestCalculatedEnergyTimeSeriesInput.SettlementMethod)
            : null;
        var acceptedGridAreas = requestCalculatedEnergyTimeSeriesInput.GridAreas.Count != 0
            ? requestCalculatedEnergyTimeSeriesInput.GridAreas
            : gridAreas;

        var acceptRequest = new RequestCalculatedEnergyTimeSeriesAcceptedV1(
            OriginalActorMessageId: requestCalculatedEnergyTimeSeriesInput.ActorMessageId,
            OriginalTransactionId: requestCalculatedEnergyTimeSeriesInput.TransactionId,
            RequestedForActorNumber: PMActorNumber.Create(requestCalculatedEnergyTimeSeriesInput.RequestedForActorNumber),
            RequestedForActorRole: PMActorRole.FromName(requestCalculatedEnergyTimeSeriesInput.RequestedForActorRole),
            RequestedByActorNumber: PMActorNumber.Create(requestCalculatedEnergyTimeSeriesInput.RequestedByActorNumber),
            RequestedByActorRole: PMActorRole.FromName(requestCalculatedEnergyTimeSeriesInput.RequestedByActorRole),
            BusinessReason: PMBusinessReason.FromName(requestCalculatedEnergyTimeSeriesInput.BusinessReason),
            PeriodStart: InstantPattern.General.Parse(requestCalculatedEnergyTimeSeriesInput.PeriodStart).Value.ToDateTimeOffset(),
            PeriodEnd: periodEnd,
            GridAreas: acceptedGridAreas ?? throw new ArgumentNullException(nameof(acceptedGridAreas), "acceptedGridAreas must be set when request has no GridAreaCodes"),
            EnergySupplierNumber: energySupplierNumber,
            BalanceResponsibleNumber: balanceResponsibleNumber,
            MeteringPointType: meteringPointType,
            SettlementMethod: settlementMethod,
            SettlementVersion: settlementVersion);

        var enqueueActorMessages = new EnqueueActorMessagesV1
        {
            OrchestrationName = Brs_026.Name,
            OrchestrationVersion = Brs_026.V1.Version,
            OrchestrationStartedByActor = new EnqueueActorMessagesActorV1
            {
                ActorNumber = requestCalculatedEnergyTimeSeriesInput.RequestedByActorNumber,
                ActorRole = PMActorRole.FromName(requestCalculatedEnergyTimeSeriesInput.RequestedByActorRole).ToActorRoleV1(),
            },
            OrchestrationInstanceId = Guid.NewGuid().ToString(),
        };
        enqueueActorMessages.SetData(acceptRequest);

        var serviceBusMessage = enqueueActorMessages.ToServiceBusMessage(
            subject: EnqueueActorMessagesV1.BuildServiceBusMessageSubject(Brs_026.Name),
            idempotencyKey: Guid.NewGuid().ToString());

        return serviceBusMessage;
    }

    public static AggregatedTimeSeriesRequestRejected GenerateRejectedFrom(AggregatedTimeSeriesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var rejectedResponse = new AggregatedTimeSeriesRequestRejected();

        var start = InstantPattern.General.Parse(request.Period.Start).Value;
        var end = InstantPattern.General.Parse(request.Period.End).Value;
        if (end <= start)
        {
            rejectedResponse.RejectReasons.Add(new RejectReason
            {
                ErrorCode = "E17",
                ErrorMessage = "Det er kun muligt at anmode om data på for en hel måned i forbindelse med en balancefiksering eller korrektioner / It is only possible to request data for a full month in relation to balancefixing or corrections",
            });
        }
        else
        {
            throw new NotImplementedException("Cannot generate rejected message for request");
        }

        return rejectedResponse;
    }

    public static ServiceBusMessage GenerateRejectedFrom(
        RequestCalculatedEnergyTimeSeriesInputV1 requestCalculatedEnergyTimeSeriesInput,
        string errorMessage,
        string errorCode)
    {
        var validationErrors = new List<ValidationErrorDto>()
        {
            new(
                errorMessage,
                errorCode),
        };
        var rejectRequest = new RequestCalculatedEnergyTimeSeriesRejectedV1(
            OriginalMessageId: requestCalculatedEnergyTimeSeriesInput.ActorMessageId,
            OriginalTransactionId: requestCalculatedEnergyTimeSeriesInput.TransactionId,
            RequestedForActorNumber: PMActorNumber.Create(requestCalculatedEnergyTimeSeriesInput.RequestedForActorNumber),
            RequestedForActorRole: PMActorRole.FromName(requestCalculatedEnergyTimeSeriesInput.RequestedForActorRole),
            RequestedByActorNumber: PMActorNumber.Create(requestCalculatedEnergyTimeSeriesInput.RequestedByActorNumber),
            RequestedByActorRole: PMActorRole.FromName(requestCalculatedEnergyTimeSeriesInput.RequestedByActorRole),
            BusinessReason: PMBusinessReason.FromName(requestCalculatedEnergyTimeSeriesInput.BusinessReason),
            validationErrors);

        var enqueueActorMessages = new EnqueueActorMessagesV1
        {
            OrchestrationName = Brs_026.Name,
            OrchestrationVersion = Brs_026.V1.Version,
            OrchestrationStartedByActor = new EnqueueActorMessagesActorV1
            {
                ActorNumber = requestCalculatedEnergyTimeSeriesInput.RequestedByActorNumber,
                ActorRole = PMActorRole.FromName(requestCalculatedEnergyTimeSeriesInput.RequestedByActorRole).ToActorRoleV1(),
            },
            OrchestrationInstanceId = Guid.NewGuid().ToString(),
        };
        enqueueActorMessages.SetData(rejectRequest);

        var serviceBusMessage = enqueueActorMessages.ToServiceBusMessage(
            subject: EnqueueActorMessagesV1.BuildServiceBusMessageSubject(Brs_026.Name),
            idempotencyKey: Guid.NewGuid().ToString());

        return serviceBusMessage;
    }

    private static TimeSeriesType GetTimeSeriesType(string? settlementMethodName, string? meteringPointTypeName)
    {
        var settlementMethodNameOrDefault = PMTypes.SettlementMethod.FromNameOrDefault(settlementMethodName);
        var meteringPointTypeNameOrDefault = PMTypes.MeteringPointType.FromNameOrDefault(meteringPointTypeName);

        return (settlementMethodNameOrDefault, meteringPointTypeNameOrDefault) switch
        {
            (var sm, var mpt) when
                sm == PMTypes.SettlementMethod.Flex
                && mpt == PMTypes.MeteringPointType.Consumption => TimeSeriesType.FlexConsumption,

            (var sm, var mpt) when
                sm == PMTypes.SettlementMethod.NonProfiled
                && mpt == PMTypes.MeteringPointType.Consumption => TimeSeriesType.NonProfiledConsumption,

            (null, var mpt) when
                mpt == PMTypes.MeteringPointType.Production => TimeSeriesType.Production,

            (null, null)
                => TimeSeriesType.FlexConsumption, // Default if no settlement method or metering point type is set

            _ => throw new NotImplementedException($"Not implemented combination of SettlementMethod and MeteringPointType ({settlementMethodName} and {meteringPointTypeName})"),
        };
    }

    private static List<TimeSeriesPoint> CreatePoints(Resolution resolution, Instant periodStart, Instant periodEnd)
    {
        var resolutionDuration = resolution switch
        {
            Resolution.Pt1H => Duration.FromHours(1),
            Resolution.Pt15M => Duration.FromMinutes(15),
            _ => throw new NotImplementedException($"Unsupported resolution in request: {resolution}"),
        };

        var points = new List<TimeSeriesPoint>();
        var currentTime = periodStart;
        while (currentTime < periodEnd)
        {
            points.Add(CreatePoint(currentTime, quantityFactor: resolution == Resolution.Pt1H ? 4 : 1));
            currentTime = currentTime.Plus(resolutionDuration);
        }

        return points;
    }

    private static TimeSeriesPoint CreatePoint(Instant currentTime, int quantityFactor = 1)
    {
        // Create random quantity between 1.00 and 999.999 (multiplied a factor used by by monthly resolution)
        var quantity = new DecimalValue { Units = Random.Shared.Next(1, 999) * quantityFactor, Nanos = Random.Shared.Next(0, 999) };

        return new TimeSeriesPoint
        {
            Time = currentTime.ToTimestamp(),
            Quantity = quantity,
            QuantityQualities = { QuantityQuality.Estimated },
        };
    }
}
