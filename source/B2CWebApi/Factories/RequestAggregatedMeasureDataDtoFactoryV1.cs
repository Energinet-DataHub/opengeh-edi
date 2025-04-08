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
using MeteringPointType = Energinet.DataHub.EDI.B2CWebApi.Models.MeteringPointType;
using SettlementMethod = Energinet.DataHub.EDI.B2CWebApi.Models.SettlementMethod;
using SettlementVersion = Energinet.DataHub.EDI.B2CWebApi.Models.SettlementVersion;

namespace Energinet.DataHub.EDI.B2CWebApi.Factories;

public static class RequestAggregatedMeasureDataDtoFactoryV1
{
    private const string AggregatedMeasureDataMessageType = "E74";
    private const string Electricity = "23";

    public static RequestAggregatedMeasureDataDto Create(
        TransactionId transactionId,
        RequestAggregatedMeasureDataMarketRequestV1 request,
        string senderNumber,
        string senderRole,
        Instant now)
    {
        ArgumentNullException.ThrowIfNull(request);

        var senderRoleCode = MapRoleNameToCode(senderRole);

        var series = new RequestAggregatedMeasureDataSeries(
            Id: transactionId.Value,
            MarketEvaluationPointType: MapEvaluationPointType(request),
            MarketEvaluationSettlementMethod: MapSettlementMethod(request),
            StartDateAndOrTimeDateTime: request.StartDate.ToString(),
            EndDateAndOrTimeDateTime: request.EndDate.ToString(),
            MeteringGridAreaDomainId: request.GridAreaCode,
            EnergySupplierMarketParticipantId: request.EnergySupplierId,
            BalanceResponsiblePartyMarketParticipantId: request.BalanceResponsibleId,
            SettlementVersion: SetSettlementVersion(request));

        return new RequestAggregatedMeasureDataDto(
            SenderNumber: senderNumber,
            SenderRoleCode: senderRoleCode,
            ReceiverNumber: DataHubDetails.DataHubActorNumber.Value,
            ReceiverRoleCode: ActorRole.MeteredDataAdministrator.Code,
            BusinessReason: MapToBusinessReasonCode(request),
            MessageType: AggregatedMeasureDataMessageType,
            MessageId: Guid.NewGuid().ToString(),
            CreatedAt: now.ToString(),
            BusinessType: Electricity,
            Serie: [series]);
    }

    private static string? SetSettlementVersion(RequestAggregatedMeasureDataMarketRequestV1 calculationType)
    {
        if (calculationType.SettlementVersion == SettlementVersion.FirstCorrection)
        {
            return "D01";
        }

        if (calculationType.SettlementVersion == SettlementVersion.SecondCorrection)
        {
            return "D02";
        }

        if (calculationType.SettlementVersion == SettlementVersion.ThirdCorrection)
        {
            return "D03";
        }

        return null;
    }

    private static string MapToBusinessReasonCode(RequestAggregatedMeasureDataMarketRequestV1 requestCalculationType)
    {
        return requestCalculationType.BusinessReason switch
        {
            Models.BusinessReason.PreliminaryAggregation => BusinessReason.PreliminaryAggregation.Code,
            Models.BusinessReason.BalanceFixing => BusinessReason.BalanceFixing.Code,
            Models.BusinessReason.WholesaleFixing => BusinessReason.WholesaleFixing.Code,
            Models.BusinessReason.Correction => BusinessReason.Correction.Code,
            _ => throw new ArgumentOutOfRangeException(nameof(requestCalculationType), requestCalculationType, "Unknown CalculationType"),
        };
    }

    private static string? MapEvaluationPointType(RequestAggregatedMeasureDataMarketRequestV1 request)
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

    private static string? MapSettlementMethod(RequestAggregatedMeasureDataMarketRequestV1 request)
    {
        switch (request.SettlementMethod)
        {
            case SettlementMethod.Flex:
                return "D01";
            case SettlementMethod.NonProfiled:
                return "E02";
        }

        return null;
    }

    private static string MapRoleNameToCode(string roleName)
    {
        ArgumentException.ThrowIfNullOrEmpty(roleName);
        var actorRole = ActorRole.FromName(roleName);

        if (actorRole == ActorRole.MeteredDataResponsible
           || actorRole == ActorRole.EnergySupplier
           || actorRole == ActorRole.BalanceResponsibleParty)
        {
            return actorRole.Code;
        }

        if (WorkaroundFlags.GridOperatorToMeteredDataResponsibleHack && actorRole == ActorRole.GridAccessProvider)
        {
            return ActorRole.MeteredDataResponsible.Code;
        }

        throw new ArgumentException($"Market Role: {actorRole}, is not allowed to request aggregated measure data.");
    }
}
