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
using Energinet.DataHub.EDI.IncomingMessages.Domain;
using Energinet.DataHub.EDI.Process.Interfaces;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Factories;

public static class InitializeAggregatedMeasureDataProcessDtoFactory
{
    public static InitializeAggregatedMeasureDataProcessDto Create(RequestAggregatedMeasureDataMessage request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var senderActorNumber = ActorNumber.Create(request.SenderNumber);
        var senderActorRole = ActorRole.FromCode(request.SenderRoleCode);

        var series = request.Series
            .Cast<RequestAggregatedMeasureDataMessageSeries>()
            .Select(
                series =>
                {
                    var gridAreas = series.DelegatedGridAreas.Count > 0
                        ? series.DelegatedGridAreas
                        : series.GridArea != null
                            ? new List<string> { series.GridArea }
                            : Array.Empty<string>();

                    return new InitializeAggregatedMeasureDataProcessSeries(
                        Id: TransactionId.From(series.TransactionId),
                        MeteringPointType: series.MeteringPointType,
                        SettlementMethod: series.SettlementMethod,
                        StartDateTime: series.StartDateTime,
                        EndDateTime: series.EndDateTime,
                        RequestedGridAreaCode: series.GridArea,
                        EnergySupplierNumber: series.EnergySupplierId,
                        BalanceResponsibleNumber: series.BalanceResponsiblePartyId,
                        SettlementVersion: series.SettlementVersion,
                        GridAreas: gridAreas,
                        RequestedByActor: RequestedByActor.From(
                            senderActorNumber,
                            series.RequestedByActorRole ?? senderActorRole),
                        OriginalActor: OriginalActor.From(
                            series.OriginalActorNumber ?? senderActorNumber,
                            senderActorRole));
                }).ToList().AsReadOnly();

        return new InitializeAggregatedMeasureDataProcessDto(
                SenderNumber: request.SenderNumber,
                SenderRoleCode: request.SenderRoleCode,
                BusinessReason: request.BusinessReason,
                MessageId: request.MessageId,
                Series: series);
    }
}
