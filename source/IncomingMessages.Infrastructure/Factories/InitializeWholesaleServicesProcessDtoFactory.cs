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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Messages;
using Energinet.DataHub.EDI.Process.Interfaces;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Factories;

public static class InitializeWholesaleServicesProcessDtoFactory
{
    public static InitializeWholesaleServicesProcessDto Create(RequestWholesaleServicesMessage wholesaleServicesMessage)
    {
        ArgumentNullException.ThrowIfNull(wholesaleServicesMessage);

        var senderActorNumber = ActorNumber.Create(wholesaleServicesMessage.SenderNumber);
        var senderActorRole = ActorRole.FromCode(wholesaleServicesMessage.SenderRoleCode);

        var series = wholesaleServicesMessage.Serie
            .Cast<RequestWholesaleServicesSeries>()
            .Select(
                series =>
                {
                    var gridAreas = series.DelegatedGridAreas.Count > 0
                        ? series.DelegatedGridAreas
                        : series.GridArea != null
                            ? new List<string> { series.GridArea }
                            : Array.Empty<string>();

                    return new InitializeWholesaleServicesSeries(
                        Id: series.TransactionId,
                        StartDateTime: series.StartDateTime,
                        EndDateTime: series.EndDateTime,
                        RequestedGridAreaCode: series.GridArea,
                        EnergySupplierId: series.EnergySupplierId,
                        SettlementVersion: series.SettlementVersion,
                        Resolution: series.Resolution,
                        ChargeOwner: series.ChargeOwner,
                        ChargeTypes: series.ChargeTypes
                            .Select(
                                chargeType => new InitializeWholesaleServicesChargeType(chargeType.Id, chargeType.Type))
                            .ToList()
                            .AsReadOnly(),
                        GridAreas: gridAreas,
                        RequestedForActorNumber: series.DelegatedByActorNumber ?? senderActorNumber,
                        RequestedByActorRole: series.DelegatedToActorRole ?? senderActorRole);
                })
            .ToList().AsReadOnly();

        return new InitializeWholesaleServicesProcessDto(
                RequestedByActorNumber: senderActorNumber,
                RequestedForActorRole: senderActorRole,
                BusinessReason: wholesaleServicesMessage.BusinessReason,
                MessageId: wholesaleServicesMessage.MessageId,
                Series: series);
    }
}
