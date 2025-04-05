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
using MeteringPointType = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.MeteringPointType;

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
            MarketEvaluationPointType: request.MeteringPointType != null ? MeteringPointType.FromName(request.MeteringPointType.Name).Code : null,
            MarketEvaluationSettlementMethod: SettlementMethod.FromName(request.SettlementMethod.Name).Code,
            StartDateAndOrTimeDateTime: request.StartDate.ToString(),
            EndDateAndOrTimeDateTime: request.EndDate.ToString(),
            MeteringGridAreaDomainId: request.GridAreaCode,
            EnergySupplierMarketParticipantId: request.EnergySupplierId?.Value,
            BalanceResponsiblePartyMarketParticipantId: request.BalanceResponsibleId?.Value,
            SettlementVersion: request.SettlementVersion != null ? SettlementVersion.FromName(request.SettlementVersion.Name).Code : null);

        return new RequestAggregatedMeasureDataDto(
            SenderNumber: senderNumber,
            SenderRoleCode: senderRoleCode,
            ReceiverNumber: DataHubDetails.DataHubActorNumber.Value,
            ReceiverRoleCode: ActorRole.MeteredDataAdministrator.Code,
            BusinessReason: BusinessReason.FromName(request.BusinessReason.Name).Code,
            MessageType: AggregatedMeasureDataMessageType,
            MessageId: Guid.NewGuid().ToString(),
            CreatedAt: now.ToString(),
            BusinessType: Electricity,
            Serie: [series]);
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
