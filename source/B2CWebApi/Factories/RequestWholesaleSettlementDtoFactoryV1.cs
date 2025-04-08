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
using BusinessReason = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.BusinessReason;
using ChargeType = Energinet.DataHub.EDI.B2CWebApi.Models.ChargeType;
using RequestWholesaleSettlementChargeType = Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models.RequestWholesaleSettlementChargeType;
using RequestWholesaleSettlementSeries = Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models.RequestWholesaleSettlementSeries;
using Resolution = Energinet.DataHub.EDI.B2CWebApi.Models.Resolution;
using SettlementVersion = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.SettlementVersion;

namespace Energinet.DataHub.EDI.B2CWebApi.Factories;

public static class RequestWholesaleSettlementDtoFactoryV1
{
    private const string WholesaleSettlementMessageType = "D21";
    private const string Electricity = "23";

    public static RequestWholesaleSettlementDto Create(
        TransactionId transactionId,
        RequestWholesaleSettlementMarketRequestV1 request,
        string senderNumber,
        string senderRole,
        Instant now)
    {
        ArgumentNullException.ThrowIfNull(request);

        var senderRoleCode = MapRoleNameToCode(senderRole);

        var chargeTypes = new List<RequestWholesaleSettlementChargeType>();
        if (request.ChargeType != null)
        {
            chargeTypes.Add(new RequestWholesaleSettlementChargeType(null, MapChargeType(request.ChargeType)));
        }

        var series = new RequestWholesaleSettlementSeries(
            transactionId.Value,
            request.StartDate.ToString(),
            request.EndDate.ToString(),
            request.GridArea,
            request.EnergySupplierId,
            MapToSettlementVersion(request),
            MapToResolution(request),
            null,
            chargeTypes);

        return new RequestWholesaleSettlementDto(
            senderNumber,
            senderRoleCode,
            DataHubDetails.DataHubActorNumber.Value,
            ActorRole.MeteredDataAdministrator.Code,
            MapToBusinessReasonCode(request),
            WholesaleSettlementMessageType,
            Guid.NewGuid().ToString(),
            now.ToString(),
            Electricity,
            new[] { series });
    }

    private static string? MapToResolution(RequestWholesaleSettlementMarketRequestV1 request)
    {
        return request.Resolution switch
        {
            Resolution.Hourly => BuildingBlocks.Domain.Models.Resolution.Hourly.Code,
            Resolution.Daily => BuildingBlocks.Domain.Models.Resolution.Daily.Code,
            Resolution.Monthly => BuildingBlocks.Domain.Models.Resolution.Monthly.Code,
            Resolution.QuarterHourly => BuildingBlocks.Domain.Models.Resolution.QuarterHourly.Code,
            _ => null,
        };
    }

    private static string? MapChargeType(ChargeType? requestChargeType)
    {
        return requestChargeType switch
        {
            ChargeType.Tariff => BuildingBlocks.Domain.Models.ChargeType.Tariff.Code,
            ChargeType.Subscription => BuildingBlocks.Domain.Models.ChargeType.Subscription.Code,
            ChargeType.Fee => BuildingBlocks.Domain.Models.ChargeType.Fee.Code,
            _ => null,
        };
    }

    private static string? MapToSettlementVersion(RequestWholesaleSettlementMarketRequestV1 calculationType)
    {
        return calculationType.SettlementVersion switch
        {
            Models.SettlementVersion.FirstCorrection => SettlementVersion.FirstCorrection.Code,
            Models.SettlementVersion.SecondCorrection => SettlementVersion.SecondCorrection.Code,
            Models.SettlementVersion.ThirdCorrection => SettlementVersion.ThirdCorrection.Code,
            _ => null,
        };
    }

    private static string MapToBusinessReasonCode(RequestWholesaleSettlementMarketRequestV1 calculationType)
    {
        return calculationType.BusinessReason switch
        {
            Models.BusinessReason.PreliminaryAggregation => BusinessReason.PreliminaryAggregation.Code,
            Models.BusinessReason.BalanceFixing => BusinessReason.BalanceFixing.Code,
            Models.BusinessReason.WholesaleFixing => BusinessReason.WholesaleFixing.Code,
            Models.BusinessReason.Correction => BusinessReason.Correction.Code,
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
