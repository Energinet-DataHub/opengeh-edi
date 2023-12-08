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
using Energinet.DataHub.EDI.MasterData.Domain.Actors;
using Energinet.DataHub.EDI.Process.Interfaces;
using NodaTime;

namespace Energinet.DataHub.EDI.B2CWebApi.Factories;

public static class RequestAggregatedMeasureDataHttpFactory
{
    public static RequestAggregatedMeasureDataDto Create(
        RequestAggregatedMeasureDataMarketRequest request,
        string senderNumber,
        string senderRole,
        DateTimeZone dateTimeZone,
        Instant now)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        var senderRoleCode = MapRoleNameToCode(senderRole);

        var serie = new Serie(
            Guid.NewGuid().ToString(),
            MapEvaluationPointType(request),
            MapSettlementMethod(request),
            InstantFormatFactory.SetInstantToMidnight(request.StartDate, dateTimeZone).ToString(),
            string.IsNullOrWhiteSpace(request.EndDate) ? null : InstantFormatFactory.SetInstantToMidnight(request.EndDate, dateTimeZone, Duration.FromMilliseconds(1)).ToString(),
            request.GridArea,
            request.EnergySupplierId,
            request.BalanceResponsibleId,
            SetSettlementSeriesVersion(request.ProcessType));

        return new RequestAggregatedMeasureDataDto(
            senderNumber,
            senderRoleCode,
            DataHubDetails.IdentificationNumber.Value,
            MarketRole.CalculationResponsibleRole.Code,
            MapToBusinessReasonCode(request.ProcessType),
            "E74",
            Guid.NewGuid().ToString(),
            now.ToString(),
            BusinessType: "23",
            new[] { serie });
    }

    private static string? SetSettlementSeriesVersion(ProcessType processType)
    {
        if (processType != ProcessType.FirstCorrection && processType != ProcessType.SecondCorrection &&
            processType != ProcessType.ThirdCorrection)
        {
            return null;
        }

        if (processType == ProcessType.FirstCorrection)
        {
            return "D01";
        }

        if (processType == ProcessType.SecondCorrection)
        {
            return "D02";
        }

        return "D03";
    }

    private static string MapToBusinessReasonCode(ProcessType requestProcessType)
    {
        return requestProcessType switch
        {
            ProcessType.PreliminaryAggregation => "D03",
            ProcessType.BalanceFixing => "D04",
            ProcessType.WholesaleFixing => "D05",
            ProcessType.FirstCorrection => "D32",
            ProcessType.SecondCorrection => "D32",
            ProcessType.ThirdCorrection => "D32",
            _ => throw new ArgumentOutOfRangeException(nameof(requestProcessType), requestProcessType, "Unknown ProcessType"),
        };
    }

    private static string? MapEvaluationPointType(RequestAggregatedMeasureDataMarketRequest request)
    {
        switch (request.MeteringPointType)
        {
            case MeteringPointType.Production:
                return "E18";
            case MeteringPointType.FlexConsumption:
            case MeteringPointType.TotalConsumption:
            case MeteringPointType.NonProfiledConsumption:
                return "E17";
            case MeteringPointType.Exchange:
                return "E20";
        }

        return null;
    }

    private static string? MapSettlementMethod(RequestAggregatedMeasureDataMarketRequest request)
    {
        switch (request.MeteringPointType)
        {
            case MeteringPointType.FlexConsumption:
                return "D01";
            case MeteringPointType.NonProfiledConsumption:
                return "E02";
        }

        return null;
    }

    private static string MapRoleNameToCode(string roleName)
    {
        ArgumentException.ThrowIfNullOrEmpty(roleName);

        if (roleName.Equals(MarketRole.MeteredDataResponsible.Name, StringComparison.OrdinalIgnoreCase))
        {
            return MarketRole.MeteredDataResponsible.Code;
        }

        if (roleName.Equals(MarketRole.EnergySupplier.Name, StringComparison.OrdinalIgnoreCase))
        {
            return MarketRole.EnergySupplier.Code;
        }

        if (roleName.Equals(MarketRole.BalanceResponsibleParty.Name, StringComparison.OrdinalIgnoreCase))
        {
            return MarketRole.BalanceResponsibleParty.Code;
        }

        throw new ArgumentException($"roleName: {roleName}. is unsupported to map to a role name");
    }
}
