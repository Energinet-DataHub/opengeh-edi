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

using Energinet.DataHub.EDI.IncomingMessages.Domain.Messages;
using Energinet.DataHub.EDI.Process.Interfaces;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Factories;

public static class RequestWholesaleServicesDtoFactory
{
    public static InitializeWholesaleServicesProcessDto Create(RequestWholesaleSettlementMessage wholesaleSettlementMessage)
    {
        ArgumentNullException.ThrowIfNull(wholesaleSettlementMessage);

        var series = wholesaleSettlementMessage.Serie
            .Cast<RequestWholesaleSettlementSerie>()
            .Select(
                serie => new RequestWholesaleServicesSerie(
                    serie.TransactionId,
                    serie.StartDateAndOrTimeDateTime,
                    serie.EndDateAndOrTimeDateTime,
                    serie.MeteringGridAreaDomainId,
                    serie.EnergySupplierMarketParticipantId,
                    serie.SettlementSeriesVersion,
                    serie.Resolution,
                    serie.ChargeOwner,
                    serie.ChargeTypes
                        .Select(
                            chargeType => new RequestWholesaleServicesChargeType(chargeType.Id, chargeType.Type))
                        .ToList().AsReadOnly()))
            .ToList().AsReadOnly();

        return new InitializeWholesaleServicesProcessDto(
                wholesaleSettlementMessage.SenderNumber,
                wholesaleSettlementMessage.SenderRoleCode,
                wholesaleSettlementMessage.ReceiverNumber,
                wholesaleSettlementMessage.ReceiverRoleCode,
                wholesaleSettlementMessage.BusinessReason,
                wholesaleSettlementMessage.MessageType,
                wholesaleSettlementMessage.MessageId,
                wholesaleSettlementMessage.CreatedAt,
                wholesaleSettlementMessage.BusinessType,
                series);
    }
}
