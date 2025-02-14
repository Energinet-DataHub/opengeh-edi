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

using System.Diagnostics.CodeAnalysis;
using Azure.Messaging.ServiceBus;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.Edi.Requests;
using Energinet.DataHub.Edi.Responses;
using Energinet.DataHub.ProcessManager.Abstractions.Contracts;
using Energinet.DataHub.ProcessManager.Components.Abstractions.BusinessValidation;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_026_028.BRS_028;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_026_028.BRS_028.V1.Model;
using Energinet.DataHub.ProcessManager.Shared.Extensions;
using NodaTime;
using NodaTime.Serialization.Protobuf;
using NodaTime.Text;
using ChargeType = Energinet.DataHub.Edi.Requests.ChargeType;
using Period = Energinet.DataHub.Edi.Responses.Period;
using PMActorNumber = Energinet.DataHub.ProcessManager.Abstractions.Core.ValueObjects.ActorNumber;
using PMActorRole = Energinet.DataHub.ProcessManager.Abstractions.Core.ValueObjects.ActorRole;
using PMBusinessReason = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects.BusinessReason;
using PMChargeType = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects.ChargeType;
using PMResolution = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects.Resolution;
using PMSettlementVersion = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects.SettlementVersion;

namespace Energinet.DataHub.EDI.IntegrationTests.EventBuilders;

public static class WholesaleServicesResponseEventBuilder
{
    /// <summary>
    /// Generate a mock WholesaleRequestAccepted response from Wholesale, based on the WholesaleServicesRequest
    /// It is very important that the generated data is correct, since assertions is based on this data
    /// </summary>
    public static WholesaleServicesRequestAccepted GenerateAcceptedFrom(
        WholesaleServicesRequest request,
        Instant now,
        string? defaultChargeOwnerId = null,
        string? defaultEnergySupplierId = null,
        ICollection<string>? defaultGridAreas = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(now);

        if (!request.HasChargeOwnerId && defaultChargeOwnerId == null)
            throw new ArgumentNullException(nameof(defaultChargeOwnerId), "defaultChargeOwnerId must be set when request has null ChargeOwnerId");
        if (!request.HasEnergySupplierId && defaultEnergySupplierId == null)
            throw new ArgumentNullException(nameof(defaultEnergySupplierId), "defaultEnergySupplierId must be set when request has null EnergySupplierId");
        if (request.GridAreaCodes.Count == 0 && defaultGridAreas == null)
            throw new ArgumentNullException(nameof(defaultGridAreas), "defaultGridAreas must be set when request has no GridAreaCodes");

        var gridAreas = request.GridAreaCodes.ToList();
        if (gridAreas.Count == 0)
            gridAreas.AddRange(defaultGridAreas!);

        // If no charge types are specified, add some default charge types representing the different charges an actor can have.
        var chargeTypes = request.ChargeTypes.ToList();
        if (chargeTypes.Count == 0)
            chargeTypes.Add(new ChargeType { ChargeCode = "12345678", ChargeType_ = DataHubNames.ChargeType.Tariff });

        var periodStart = InstantPattern.General.Parse(request.PeriodStart).Value;
        var periodEnd = InstantPattern.General.Parse(request.PeriodEnd).Value;

        var series = gridAreas.SelectMany(
            ga =>
            {
                var series = chargeTypes.Select(
                    ct =>
                    {
                        var resolution = request.Resolution == DataHubNames.Resolution.Monthly
                            ? WholesaleServicesRequestSeries.Types.Resolution.Monthly
                            : WholesaleServicesRequestSeries.Types.Resolution.Hour;

                        var points = CreatePoints(resolution, periodStart, periodEnd);

                        var series = new WholesaleServicesRequestSeries()
                        {
                            Currency = WholesaleServicesRequestSeries.Types.Currency.Dkk,
                            Period = new Period
                            {
                                StartOfPeriod = periodStart.ToTimestamp(), EndOfPeriod = periodEnd.ToTimestamp(),
                            },
                            Resolution = resolution,
                            CalculationType = request.BusinessReason == DataHubNames.BusinessReason.WholesaleFixing
                                ? WholesaleServicesRequestSeries.Types.CalculationType.WholesaleFixing
                                : throw new NotImplementedException(
                                    "Builder only supports WholesaleFixing, not corrections"),
                            ChargeType =
                                Enum.TryParse<WholesaleServicesRequestSeries.Types.ChargeType>(
                                    ct.ChargeType_,
                                    out var result)
                                    ? result
                                    : throw new NotImplementedException("Unsupported chargetype in request"),
                            ChargeOwnerId = request.HasChargeOwnerId ? request.ChargeOwnerId : defaultChargeOwnerId!,
                            GridArea = ga,
                            QuantityUnit = WholesaleServicesRequestSeries.Types.QuantityUnit.Kwh,
                            SettlementMethod = WholesaleServicesRequestSeries.Types.SettlementMethod.Flex,
                            EnergySupplierId =
                                request.HasEnergySupplierId ? request.EnergySupplierId : defaultEnergySupplierId!,
                            MeteringPointType = WholesaleServicesRequestSeries.Types.MeteringPointType.Consumption,
                            CalculationResultVersion = now.ToUnixTimeTicks(),
                        };

                        if (ct.HasChargeCode)
                            series.ChargeCode = ct.ChargeCode;
                        series.TimeSeriesPoints.AddRange(points);

                        return series;
                    }).ToList();

                // When the resolution is monthly and no charge types are specified, series should contain a total monthly amount result.
                if (request.Resolution == DataHubNames.Resolution.Monthly
                    && request.ChargeTypes.Count == 0)
                {
                    var totalMonthlyAmountSeries = new WholesaleServicesRequestSeries()
                    {
                        Currency = WholesaleServicesRequestSeries.Types.Currency.Dkk,
                        Period = new Period
                        {
                            StartOfPeriod = periodStart.ToTimestamp(),
                            EndOfPeriod = periodEnd.ToTimestamp(),
                        },
                        Resolution = WholesaleServicesRequestSeries.Types.Resolution.Monthly,
                        CalculationType = request.BusinessReason == DataHubNames.BusinessReason.WholesaleFixing
                            ? WholesaleServicesRequestSeries.Types.CalculationType.WholesaleFixing
                            : throw new NotImplementedException("Builder only supports WholesaleFixing, not corrections"),
                        ChargeOwnerId = request.HasChargeOwnerId ? request.ChargeOwnerId : defaultChargeOwnerId!,
                        GridArea = ga,
                        EnergySupplierId = request.HasEnergySupplierId ? request.EnergySupplierId : defaultEnergySupplierId!,
                        CalculationResultVersion = now.ToUnixTimeTicks(),
                    };

                    var totalMonthlyAmountSeriesPoints = new WholesaleServicesRequestSeries.Types.Point
                    {
                        Time = periodStart.ToTimestamp(), Amount = DecimalValue.FromDecimal(999),
                    };
                    totalMonthlyAmountSeries.TimeSeriesPoints.Add(totalMonthlyAmountSeriesPoints);
                    series.Add(totalMonthlyAmountSeries);
                }

                return series;
            });

        var requestAcceptedMessage = new WholesaleServicesRequestAccepted();
        requestAcceptedMessage.Series.AddRange(series);

        return requestAcceptedMessage;
    }

    /// <summary>
    /// Accepted Response from PM
    /// </summary>
    /// <param name="requestCalculatedWholesaleServicesInputV1"></param>
    /// <param name="gridAreas">All grid areas which PM finds for requester, when request is not limited by gridAreas</param>
    /// <param name="defaultChargeOwnerId"></param>
    /// <param name="defaultEnergySupplierId"></param>
    public static ServiceBusMessage GenerateAcceptedFrom(
        RequestCalculatedWholesaleServicesInputV1 requestCalculatedWholesaleServicesInputV1,
        IReadOnlyCollection<string>? gridAreas = null,
        string? defaultChargeOwnerId = null,
        string? defaultEnergySupplierId = null)
    {
        var chargeTypes = requestCalculatedWholesaleServicesInputV1.ChargeTypes?
            .Select(x => new RequestCalculatedWholesaleServicesAcceptedV1.AcceptedChargeType(PMChargeType.FromName(x.ChargeType!), x.ChargeCode))
            ?? new List<RequestCalculatedWholesaleServicesAcceptedV1.AcceptedChargeType>() { new(PMChargeType.Tariff, "25361478") };

        var periodEnd = requestCalculatedWholesaleServicesInputV1.PeriodEnd != null ?
            InstantPattern.General.Parse(requestCalculatedWholesaleServicesInputV1.PeriodEnd).Value.ToDateTimeOffset()
            : throw new ArgumentNullException(nameof(requestCalculatedWholesaleServicesInputV1.PeriodEnd), "PeriodEnd must be set");
        var energySupplierNumber = requestCalculatedWholesaleServicesInputV1.EnergySupplierNumber != null
            ? PMActorNumber.Create(requestCalculatedWholesaleServicesInputV1.EnergySupplierNumber)
            : defaultEnergySupplierId != null ? PMActorNumber.Create(defaultEnergySupplierId) : null;
        var chargeOwnerNumber = requestCalculatedWholesaleServicesInputV1.ChargeOwnerNumber != null
            ? PMActorNumber.Create(requestCalculatedWholesaleServicesInputV1.ChargeOwnerNumber)
            : defaultChargeOwnerId != null ? PMActorNumber.Create(defaultChargeOwnerId) : null;
        var settlementVersion = requestCalculatedWholesaleServicesInputV1.SettlementVersion != null
            ? PMSettlementVersion.FromName(requestCalculatedWholesaleServicesInputV1.SettlementVersion)
            : null;
        var resolution = requestCalculatedWholesaleServicesInputV1.Resolution != null
            ? PMResolution.FromName(requestCalculatedWholesaleServicesInputV1.Resolution)
            : null;
        var acceptedGridAreas = requestCalculatedWholesaleServicesInputV1.GridAreas.Count != 0
            ? requestCalculatedWholesaleServicesInputV1.GridAreas
            : gridAreas;
        var acceptRequest = new RequestCalculatedWholesaleServicesAcceptedV1(
            OriginalActorMessageId: requestCalculatedWholesaleServicesInputV1.ActorMessageId,
            OriginalTransactionId: requestCalculatedWholesaleServicesInputV1.TransactionId,
            RequestedForActorNumber: PMActorNumber.Create(requestCalculatedWholesaleServicesInputV1.RequestedForActorNumber),
            RequestedForActorRole: PMActorRole.FromName(requestCalculatedWholesaleServicesInputV1.RequestedForActorRole),
            RequestedByActorNumber: PMActorNumber.Create(requestCalculatedWholesaleServicesInputV1.RequestedByActorNumber),
            RequestedByActorRole: PMActorRole.FromName(requestCalculatedWholesaleServicesInputV1.RequestedByActorRole),
            BusinessReason: PMBusinessReason.FromName(requestCalculatedWholesaleServicesInputV1.BusinessReason),
            Resolution: resolution,
            PeriodStart: InstantPattern.General.Parse(requestCalculatedWholesaleServicesInputV1.PeriodStart).Value.ToDateTimeOffset(),
            PeriodEnd: periodEnd,
            GridAreas: acceptedGridAreas ?? throw new ArgumentNullException(nameof(acceptedGridAreas), "acceptedGridAreas must be set when request has no GridAreaCodes"),
            EnergySupplierNumber: energySupplierNumber,
            ChargeOwnerNumber: chargeOwnerNumber,
            SettlementVersion: settlementVersion,
            ChargeTypes: chargeTypes.ToList());

        var enqueueActorMessages = new EnqueueActorMessagesV1
        {
            OrchestrationName = Brs_028.Name,
            OrchestrationVersion = Brs_028.V1.Version,
            OrchestrationStartedByActor = new EnqueueActorMessagesActorV1
            {
                ActorNumber = requestCalculatedWholesaleServicesInputV1.RequestedByActorNumber,
                ActorRole = PMActorRole.FromName(requestCalculatedWholesaleServicesInputV1.RequestedByActorRole).ToActorRoleV1(),
            },
            OrchestrationInstanceId = Guid.NewGuid().ToString(),
        };
        enqueueActorMessages.SetData(acceptRequest);

        var serviceBusMessage = enqueueActorMessages.ToServiceBusMessage(
            subject: EnqueueActorMessagesV1.BuildServiceBusMessageSubject(Brs_028.Name),
            idempotencyKey: Guid.NewGuid().ToString());

        return serviceBusMessage;
    }

    public static WholesaleServicesRequestRejected GenerateRejectedFrom(WholesaleServicesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var rejectedMessage = new WholesaleServicesRequestRejected();

        var start = InstantPattern.General.Parse(request.PeriodStart).Value;
        var end = InstantPattern.General.Parse(request.PeriodEnd).Value;
        if (end <= start)
        {
            rejectedMessage.RejectReasons.Add(new RejectReason
            {
                ErrorCode = "E17",
                ErrorMessage = "Det er kun muligt at anmode om data på for en hel måned i forbindelse med en engrosfiksering eller korrektioner / It is only possible to request data for a full month in relation to wholesalefixing or corrections",
            });
        }
        else if (request.EnergySupplierId != request.RequestedForActorNumber)
        {
            rejectedMessage.RejectReasons.Add(new RejectReason
            {
                ErrorCode = "E16",
                ErrorMessage = "Elleverandør i header og payload stemmer ikke overens / Energysupplier in header and payload must be the same",
            });
        }
        else
        {
            throw new NotImplementedException("Cannot generate rejected message for request");
        }

        return rejectedMessage;
    }

    public static ServiceBusMessage GenerateRejectedFrom(
        RequestCalculatedWholesaleServicesInputV1 requestCalculatedWholesaleServicesInputV1,
        string errorMessage,
        string errorCode)
    {
        var validationErrors = new List<ValidationErrorDto>()
        {
            new(
                errorMessage,
                errorCode),
        };
        var rejectRequest = new RequestCalculatedWholesaleServicesRejectedV1(
            OriginalMessageId: requestCalculatedWholesaleServicesInputV1.ActorMessageId,
            OriginalTransactionId: requestCalculatedWholesaleServicesInputV1.TransactionId,
            RequestedForActorNumber: PMActorNumber.Create(requestCalculatedWholesaleServicesInputV1.RequestedForActorNumber),
            RequestedForActorRole: PMActorRole.FromName(requestCalculatedWholesaleServicesInputV1.RequestedForActorRole),
            RequestedByActorNumber: PMActorNumber.Create(requestCalculatedWholesaleServicesInputV1.RequestedByActorNumber),
            RequestedByActorRole: PMActorRole.FromName(requestCalculatedWholesaleServicesInputV1.RequestedByActorRole),
            BusinessReason: PMBusinessReason.FromName(requestCalculatedWholesaleServicesInputV1.BusinessReason),
            validationErrors);

        var enqueueActorMessages = new EnqueueActorMessagesV1
        {
            OrchestrationName = Brs_028.Name,
            OrchestrationVersion = Brs_028.V1.Version,
            OrchestrationStartedByActor = new EnqueueActorMessagesActorV1
            {
                ActorNumber = requestCalculatedWholesaleServicesInputV1.RequestedByActorNumber,
                ActorRole = PMActorRole.FromName(requestCalculatedWholesaleServicesInputV1.RequestedByActorRole).ToActorRoleV1(),
            },
            OrchestrationInstanceId = Guid.NewGuid().ToString(), // TODO, could be used to assert on when notifying the orchestration instance in pm
        };
        enqueueActorMessages.SetData(rejectRequest);

        var serviceBusMessage = enqueueActorMessages.ToServiceBusMessage(
            subject: EnqueueActorMessagesV1.BuildServiceBusMessageSubject(Brs_028.Name),
            idempotencyKey: Guid.NewGuid().ToString());

        return serviceBusMessage;
    }

    private static List<WholesaleServicesRequestSeries.Types.Point> CreatePoints(WholesaleServicesRequestSeries.Types.Resolution resolution, Instant periodStart, Instant periodEnd)
    {
        var points = new List<WholesaleServicesRequestSeries.Types.Point>();

        if (resolution == WholesaleServicesRequestSeries.Types.Resolution.Monthly)
        {
            points.Add(CreatePoint(periodEnd, resolution,  quantityFactor: 30 * 24));
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
                points.Add(CreatePoint(currentTime, resolution));
                currentTime = currentTime.Plus(resolutionDuration);
            }
        }

        return points;
    }

    [SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "Random not used for security")]
    private static WholesaleServicesRequestSeries.Types.Point CreatePoint(Instant currentTime, WholesaleServicesRequestSeries.Types.Resolution resolution, int quantityFactor = 1)
    {
        // Create random price between 0.99 and 5.99
        var price = new DecimalValue { Units = Random.Shared.Next(0, 4), Nanos = Random.Shared.Next(1, 99) };

        // Create random quantity between 1.00 and 999.99 (multiplied a factor used by monthly resolution)
        var quantity = new DecimalValue { Units = Random.Shared.Next(1, 999) * quantityFactor, Nanos = Random.Shared.Next(0, 99) };

        // Calculate the total amount (price * quantity)
        var totalAmount = price.ToDecimal() * quantity.ToDecimal();

        var point = new WholesaleServicesRequestSeries.Types.Point
        {
            Time = currentTime.ToTimestamp(),
            Price = price,
            Quantity = quantity,
            Amount = DecimalValue.FromDecimal(totalAmount),
        };

        if (resolution != WholesaleServicesRequestSeries.Types.Resolution.Monthly)
            point.QuantityQualities.Add(QuantityQuality.Calculated);

        return point;
    }
}
