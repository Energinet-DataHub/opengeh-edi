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

using Energinet.DataHub.EDI.B2CWebApi.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;
using NodaTime;
using ActorRole = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.ActorRole;
using RequestWholesaleSettlementChargeType = Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models.RequestWholesaleSettlementChargeType;

namespace Energinet.DataHub.EDI.B2CWebApi.Factories;

public static class RequestWholesaleSettlementDtoFactory
{
    private const string WholesaleSettlementMessageType = "D21";
    private const string Electricity = "23";

    public static RequestWholesaleSettlementDto Create(
        RequestWholesaleSettlementMarketRequest request,
        string senderNumber,
        string senderRole,
        DateTimeZone dateTimeZone,
        Instant now)
    {
        ArgumentNullException.ThrowIfNull(request);

        var senderRoleCode = MapRoleNameToCode(senderRole);

        // TODO: Remove request.Resolution when PriceType is fully implemented in the UI
        #pragma warning disable CS0618 // Type or member is obsolete
        var (resolution, chargeType) = MapToResolutionAndChargeType(request.PriceType, request.Resolution);
        #pragma warning restore CS0618 // Type or member is obsolete

        List<RequestWholesaleSettlementChargeType> chargeTypes = chargeType is not null
            ? [new(null, chargeType.Code)]
            : [];

        var series = new RequestWholesaleSettlementSeries(
            TransactionId.New().Value,
            InstantFormatFactory.SetInstantToMidnight(request.StartDate, dateTimeZone).ToString(),
            string.IsNullOrWhiteSpace(request.EndDate) ? null : InstantFormatFactory.SetInstantToMidnight(request.EndDate, dateTimeZone, Duration.FromMilliseconds(1)).ToString(),
            request.GridArea,
            request.EnergySupplierId,
            MapToSettlementVersion(request.CalculationType),
            resolution,
            null,
            chargeTypes);

        return new RequestWholesaleSettlementDto(
            senderNumber,
            senderRoleCode,
            DataHubDetails.DataHubActorNumber.Value,
            ActorRole.MeteredDataAdministrator.Code,
            MapToBusinessReasonCode(request.CalculationType),
            WholesaleSettlementMessageType,
            Guid.NewGuid().ToString(),
            now.ToString(),
            Electricity,
            new[] { series });
    }

    private static (string? Resolution, ChargeType? ChargeType) MapToResolutionAndChargeType(PriceType? priceType, string? resolution)
    {
        if (priceType is null && resolution is null)
            return (null, null);

        if (priceType is not null)
        {
            return priceType switch
            {
                PriceType.TariffSubscriptionAndFee => (null, null),
                PriceType.Tariff => (null, ChargeType.Tariff),
                PriceType.Subscription => (null, ChargeType.Subscription),
                PriceType.Fee => (null, ChargeType.Fee),
                PriceType.MonthlyTariff => (Resolution.Monthly.Code, ChargeType.Tariff),
                PriceType.MonthlySubscription => (Resolution.Monthly.Code, ChargeType.Subscription),
                PriceType.MonthlyFee => (Resolution.Monthly.Code, ChargeType.Fee),
                PriceType.MonthlyTariffSubscriptionAndFee => (Resolution.Monthly.Code, null),
                _ => throw new ArgumentOutOfRangeException(nameof(priceType), priceType, "Unknown PriceType"),
            };
        }

        return (resolution, null);
    }

    private static string? MapToSettlementVersion(CalculationType calculationType)
    {
        return calculationType switch
        {
            CalculationType.FirstCorrection => SettlementVersion.FirstCorrection.Code,
            CalculationType.SecondCorrection => SettlementVersion.SecondCorrection.Code,
            CalculationType.ThirdCorrection => SettlementVersion.ThirdCorrection.Code,
            _ => null,
        };
    }

    private static string MapToBusinessReasonCode(CalculationType calculationType)
    {
        return calculationType switch
        {
            CalculationType.PreliminaryAggregation => BusinessReason.PreliminaryAggregation.Code,
            CalculationType.BalanceFixing => BusinessReason.BalanceFixing.Code,
            CalculationType.WholesaleFixing => BusinessReason.WholesaleFixing.Code,
            CalculationType.FirstCorrection => BusinessReason.Correction.Code,
            CalculationType.SecondCorrection => BusinessReason.Correction.Code,
            CalculationType.ThirdCorrection => BusinessReason.Correction.Code,
            _ => throw new ArgumentOutOfRangeException(
                nameof(calculationType),
                calculationType,
                "Unknown CalculationType"),
        };
    }

    private static string MapRoleNameToCode(string roleName)
    {
        ArgumentException.ThrowIfNullOrEmpty(roleName);
        var actorRole = ActorRole.FromName(roleName);

        if (actorRole == ActorRole.SystemOperator
             || actorRole == ActorRole.EnergySupplier
             || actorRole == ActorRole.GridAccessProvider)
        {
            return actorRole.Code;
        }

        throw new ArgumentException($"Market Role: {actorRole}, is not allowed to request wholesale settlement.");
    }
}
