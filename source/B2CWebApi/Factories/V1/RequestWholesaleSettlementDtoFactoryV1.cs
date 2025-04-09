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
using ChargeType = Energinet.DataHub.EDI.B2CWebApi.Models.V1.ChargeType;
using RequestWholesaleSettlementChargeType = Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models.RequestWholesaleSettlementChargeType;
using RequestWholesaleSettlementSeries = Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models.RequestWholesaleSettlementSeries;
using Resolution = Energinet.DataHub.EDI.B2CWebApi.Models.V1.Resolution;
using SettlementVersion = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.SettlementVersion;

namespace Energinet.DataHub.EDI.B2CWebApi.Factories.V1;

public static class RequestWholesaleSettlementDtoFactoryV1
{
    private const string WholesaleSettlementMessageType = "D21";
    private const string Electricity = "23";

    public static RequestWholesaleSettlementDto Create(
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
            chargeTypes.Add(new RequestWholesaleSettlementChargeType(null, MapChargeTypeCode(request.ChargeType)));
        }

        var series = new RequestWholesaleSettlementSeries(
            Id: TransactionId.New().Value,
            StartDateAndOrTimeDateTime: request.StartDate.ToString(),
            EndDateAndOrTimeDateTime: request.EndDate.ToString(),
            MeteringGridAreaDomainId: request.GridArea,
            EnergySupplierMarketParticipantId: request.EnergySupplierId,
            SettlementVersion: MapSettlementVersionCode(request),
            Resolution: MapResolutionCode(request),
            ChargeOwner: null,
            ChargeTypes: chargeTypes);

        return new RequestWholesaleSettlementDto(
            SenderNumber: senderNumber,
            SenderRoleCode: senderRoleCode,
            ReceiverNumber: DataHubDetails.DataHubActorNumber.Value,
            ReceiverRoleCode: ActorRole.MeteredDataAdministrator.Code,
            BusinessReason: MapBusinessReasonCode(request),
            MessageType: WholesaleSettlementMessageType,
            MessageId: MessageId.New().Value,
            CreatedAt: now.ToString(),
            BusinessType: Electricity,
            Series: [series]);
    }

    private static string? MapResolutionCode(RequestWholesaleSettlementMarketRequestV1 request)
    {
        return request.Resolution switch
        {
            Resolution.Monthly => BuildingBlocks.Domain.Models.Resolution.Monthly.Code,
            _ => null,
        };
    }

    private static string? MapChargeTypeCode(ChargeType? requestChargeType)
    {
        return requestChargeType switch
        {
            ChargeType.Tariff => BuildingBlocks.Domain.Models.ChargeType.Tariff.Code,
            ChargeType.Subscription => BuildingBlocks.Domain.Models.ChargeType.Subscription.Code,
            ChargeType.Fee => BuildingBlocks.Domain.Models.ChargeType.Fee.Code,
            _ => null,
        };
    }

    private static string? MapSettlementVersionCode(RequestWholesaleSettlementMarketRequestV1 calculationType)
    {
        return calculationType.SettlementVersion switch
        {
            Models.V1.SettlementVersion.FirstCorrection => SettlementVersion.FirstCorrection.Code,
            Models.V1.SettlementVersion.SecondCorrection => SettlementVersion.SecondCorrection.Code,
            Models.V1.SettlementVersion.ThirdCorrection => SettlementVersion.ThirdCorrection.Code,
            _ => null,
        };
    }

    private static string MapBusinessReasonCode(RequestWholesaleSettlementMarketRequestV1 request)
    {
        return request.BusinessReason switch
        {
            Models.V1.BusinessReason.PreliminaryAggregation => BusinessReason.PreliminaryAggregation.Code,
            Models.V1.BusinessReason.BalanceFixing => BusinessReason.BalanceFixing.Code,
            Models.V1.BusinessReason.WholesaleFixing => BusinessReason.WholesaleFixing.Code,
            Models.V1.BusinessReason.Correction => BusinessReason.Correction.Code,
            _ => throw new ArgumentOutOfRangeException(
                nameof(request),
                request,
                "Unknown BusinessReason"),
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
