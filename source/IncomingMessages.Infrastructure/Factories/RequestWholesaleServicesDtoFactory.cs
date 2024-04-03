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
    public static InitializeWholesaleServicesProcessDto Create(RequestWholesaleServicesMessage wholesaleServicesMessage)
    {
        ArgumentNullException.ThrowIfNull(wholesaleServicesMessage);

        var series = wholesaleServicesMessage.Serie
            .Cast<RequestWholesaleServicesSerie>()
            .Select(
                serie => new InitializeWholesaleServicesSerie(
                    serie.TransactionId,
                    serie.StartDateTime,
                    serie.EndDateTime,
                    serie.GridArea,
                    serie.EnergySupplierId,
                    serie.SettlementVersion,
                    serie.Resolution,
                    serie.ChargeOwner,
                    serie.ChargeTypes
                        .Select(
                            chargeType => new InitializeWholesaleServicesChargeType(chargeType.Id, chargeType.Type))
                        .ToList().AsReadOnly()))
            .ToList().AsReadOnly();

        return new InitializeWholesaleServicesProcessDto(
                wholesaleServicesMessage.SenderNumber,
                wholesaleServicesMessage.SenderRoleCode,
                wholesaleServicesMessage.ReceiverNumber,
                wholesaleServicesMessage.ReceiverRoleCode,
                wholesaleServicesMessage.BusinessReason,
                wholesaleServicesMessage.MessageType,
                wholesaleServicesMessage.MessageId,
                wholesaleServicesMessage.CreatedAt,
                wholesaleServicesMessage.BusinessType,
                series);
    }
}
