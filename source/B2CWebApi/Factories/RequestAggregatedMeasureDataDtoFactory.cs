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
using MeteringPointType = Energinet.DataHub.EDI.B2CWebApi.Models.MeteringPointType;

namespace Energinet.DataHub.EDI.B2CWebApi.Factories;

public static class RequestAggregatedMeasureDataDtoFactory
{
    private const string AggregatedMeasureDataMessageType = "E74";
    private const string Electricity = "23";

    public static RequestAggregatedMeasureDataDto Create(
        RequestAggregatedMeasureDataMarketRequest request,
        string senderNumber,
        string senderRole,
        DateTimeZone dateTimeZone,
        Instant now,
        string? originalTransactionIdReference = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        var senderRoleCode = MapRoleNameToCode(senderRole);

        var series = new RequestAggregatedMeasureDataSeries(
            originalTransactionIdReference ?? TransactionId.New().Value,
            MapEvaluationPointType(request),
            MapSettlementMethod(request),
            InstantFormatFactory.SetInstantToMidnight(request.StartDate, dateTimeZone).ToString(),
            string.IsNullOrWhiteSpace(request.EndDate) ? null : InstantFormatFactory.SetInstantToMidnight(request.EndDate, dateTimeZone, Duration.FromMilliseconds(1)).ToString(),
            request.GridArea,
            request.EnergySupplierId,
            request.BalanceResponsibleId,
            SetSettlementVersion(request.CalculationType));

        return new RequestAggregatedMeasureDataDto(
            senderNumber,
            senderRoleCode,
            DataHubDetails.DataHubActorNumber.Value,
            ActorRole.MeteredDataAdministrator.Code,
            MapToBusinessReasonCode(request.CalculationType),
            AggregatedMeasureDataMessageType,
            Guid.NewGuid().ToString(),
            now.ToString(),
            Electricity,
            new[] { series });
    }

    private static string? SetSettlementVersion(CalculationType calculationType)
    {
        if (calculationType == CalculationType.FirstCorrection)
        {
            return "D01";
        }

        if (calculationType == CalculationType.SecondCorrection)
        {
            return "D02";
        }

        if (calculationType == CalculationType.ThirdCorrection)
        {
            return "D03";
        }

        return null;
    }

    private static string MapToBusinessReasonCode(CalculationType requestCalculationType)
    {
        return requestCalculationType switch
        {
            CalculationType.PreliminaryAggregation => BusinessReason.PreliminaryAggregation.Code,
            CalculationType.BalanceFixing => BusinessReason.BalanceFixing.Code,
            CalculationType.WholesaleFixing => BusinessReason.WholesaleFixing.Code,
            CalculationType.FirstCorrection => BusinessReason.Correction.Code,
            CalculationType.SecondCorrection => BusinessReason.Correction.Code,
            CalculationType.ThirdCorrection => BusinessReason.Correction.Code,
            _ => throw new ArgumentOutOfRangeException(nameof(requestCalculationType), requestCalculationType, "Unknown CalculationType"),
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
        var marketRole = MarketRole.FromName(roleName);

        if ((marketRole == MarketRole.MeteredDataResponsible
           || marketRole == MarketRole.EnergySupplier
           || marketRole == MarketRole.BalanceResponsibleParty)
            && marketRole.Code != null)
        {
            return marketRole.Code;
        }

        throw new ArgumentException($"Market Role: {marketRole}, is not allowed to request aggregated measure data.");
    }
}
