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

using Energinet.DataHub.EDI.B2CWebApi.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;
using NodaTime;
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

        var series = new RequestWholesaleSettlementSeries(
            TransactionId.New().Value,
            InstantFormatFactory.SetInstantToMidnight(request.StartDate, dateTimeZone).ToString(),
            string.IsNullOrWhiteSpace(request.EndDate) ? null : InstantFormatFactory.SetInstantToMidnight(request.EndDate, dateTimeZone, Duration.FromMilliseconds(1)).ToString(),
            request.GridArea,
            request.EnergySupplierId,
            MapToSettlementVersion(request.CalculationType),
            request.Resolution,
            request.ChargeOwner,
            request.ChargeTypes.Select(ct => new RequestWholesaleSettlementChargeType(ct.Id, ct.Type)).ToList());

        return new RequestWholesaleSettlementDto(
            senderNumber,
            senderRoleCode,
            DataHubDetails.DataHubActorNumber.Value,
            MarketRole.CalculationResponsibleRole.Code,
            MapToBusinessReasonCode(request.CalculationType),
            WholesaleSettlementMessageType,
            Guid.NewGuid().ToString(),
            now.ToString(),
            Electricity,
            new[] { series });
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

        if (roleName.Equals(MarketRole.SystemOperator.Name, StringComparison.OrdinalIgnoreCase))
        {
            return MarketRole.SystemOperator.Code;
        }

        if (roleName.Equals(MarketRole.EnergySupplier.Name, StringComparison.OrdinalIgnoreCase))
        {
            return MarketRole.EnergySupplier.Code;
        }

        if (roleName.Equals(MarketRole.GridAccessProvider.Name, StringComparison.OrdinalIgnoreCase))
        {
            return MarketRole.GridAccessProvider.Code;
        }

        throw new ArgumentException($"roleName: {roleName}. is unsupported to map to a role name");
    }
}
