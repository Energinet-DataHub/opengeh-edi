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

using Energinet.DataHub.EDI.B2CWebApi.Models.V1;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;
using NodaTime;
using ActorRole = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.ActorRole;
using BusinessReason = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.BusinessReason;
using MeteringPointType = Energinet.DataHub.EDI.B2CWebApi.Models.V1.MeteringPointType;
using SettlementMethod = Energinet.DataHub.EDI.B2CWebApi.Models.V1.SettlementMethod;
using SettlementVersion = Energinet.DataHub.EDI.B2CWebApi.Models.V1.SettlementVersion;

namespace Energinet.DataHub.EDI.B2CWebApi.Factories.V1;

public static class RequestAggregatedMeasureDataDtoFactoryV1
{
    private const string AggregatedMeasureDataMessageType = "E74";
    private const string Electricity = "23";

    public static RequestAggregatedMeasureDataDto Create(
        RequestAggregatedMeasureDataMarketRequestV1 request,
        string senderNumber,
        string senderRole,
        Instant now)
    {
        ArgumentNullException.ThrowIfNull(request);

        var senderRoleCode = MapRoleNameToCode(senderRole);

        var series = new RequestAggregatedMeasureDataSeries(
            Id: TransactionId.New().Value,
            MarketEvaluationPointType: MapEvaluationPointTypeCode(request),
            MarketEvaluationSettlementMethod: MapSettlementMethodCode(request),
            StartDateAndOrTimeDateTime: request.StartDate.ToString(),
            EndDateAndOrTimeDateTime: request.EndDate.ToString(),
            MeteringGridAreaDomainId: request.GridAreaCode,
            EnergySupplierMarketParticipantId: request.EnergySupplierId,
            BalanceResponsiblePartyMarketParticipantId: request.BalanceResponsibleId,
            SettlementVersion: MapSettlementVersionCode(request));

        return new RequestAggregatedMeasureDataDto(
            SenderNumber: senderNumber,
            SenderRoleCode: senderRoleCode,
            ReceiverNumber: DataHubDetails.DataHubActorNumber.Value,
            ReceiverRoleCode: ActorRole.MeteredDataAdministrator.Code,
            BusinessReason: MapBusinessReasonCode(request),
            MessageType: AggregatedMeasureDataMessageType,
            MessageId: MessageId.New().Value,
            CreatedAt: now.ToString(),
            BusinessType: Electricity,
            Serie: [series]);
    }

    private static string? MapSettlementVersionCode(RequestAggregatedMeasureDataMarketRequestV1 request)
    {
        return request.SettlementVersion switch
        {
            SettlementVersion.SecondCorrection => BuildingBlocks.Domain.Models.SettlementVersion.SecondCorrection.Code,
            SettlementVersion.ThirdCorrection => BuildingBlocks.Domain.Models.SettlementVersion.ThirdCorrection.Code,
            SettlementVersion.FirstCorrection => BuildingBlocks.Domain.Models.SettlementVersion.FirstCorrection.Code,
            _ => null,
        };
    }

    private static string MapBusinessReasonCode(RequestAggregatedMeasureDataMarketRequestV1 request)
    {
        return request.BusinessReason switch
        {
            Models.V1.BusinessReason.PreliminaryAggregation => BusinessReason.PreliminaryAggregation.Code,
            Models.V1.BusinessReason.BalanceFixing => BusinessReason.BalanceFixing.Code,
            Models.V1.BusinessReason.WholesaleFixing => BusinessReason.WholesaleFixing.Code,
            Models.V1.BusinessReason.Correction => BusinessReason.Correction.Code,
            _ => throw new ArgumentOutOfRangeException(nameof(request), request, "Unknown BusinessReason"),
        };
    }

    private static string? MapEvaluationPointTypeCode(RequestAggregatedMeasureDataMarketRequestV1 request)
    {
        switch (request.MeteringPointType)
        {
            case MeteringPointType.Production:
                return BuildingBlocks.Domain.Models.MeteringPointType.Production.Code;
            case MeteringPointType.Consumption:
                return BuildingBlocks.Domain.Models.MeteringPointType.Consumption.Code;
            case MeteringPointType.Exchange:
                return BuildingBlocks.Domain.Models.MeteringPointType.Exchange.Code;
        }

        return null;
    }

    private static string? MapSettlementMethodCode(RequestAggregatedMeasureDataMarketRequestV1 request)
    {
        switch (request.SettlementMethod)
        {
            case SettlementMethod.Flex:
                return BuildingBlocks.Domain.Models.SettlementMethod.Flex.Code;
            case SettlementMethod.NonProfiled:
                return BuildingBlocks.Domain.Models.SettlementMethod.NonProfiled.Code;
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
