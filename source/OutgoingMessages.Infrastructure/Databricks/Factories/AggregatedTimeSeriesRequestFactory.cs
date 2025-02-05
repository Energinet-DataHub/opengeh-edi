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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults.Mappers;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults.Request;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_026_028.BRS_026.V1.Model;
using NodaTime.Text;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.Factories;

public static class AggregatedTimeSeriesRequestFactory
{
    public static AggregatedTimeSeriesRequest Parse(RequestCalculatedEnergyTimeSeriesAcceptedV1 request)
    {
        return new AggregatedTimeSeriesRequest(
            Period: new Period(
                InstantPattern.General.Parse(request.PeriodStart.ToString()).Value,
                InstantPattern.General.Parse(request.PeriodEnd.ToString()).Value),
            GetTimeSeriesTypes(request),
            MapAggregationPerRoleAndGridArea(request),
            RequestedCalculationTypeMapper.ToRequestedCalculationType(request.BusinessReason.Name, request.SettlementVersion?.Name));
    }

    private static TimeSeriesType[] GetTimeSeriesTypes(
        RequestCalculatedEnergyTimeSeriesAcceptedV1 request)
    {
        return request.MeteringPointType != null
            ? [TimeSeriesTypeMapper.MapTimeSeriesType(request.MeteringPointType.Name, request.SettlementMethod?.Name)]
            : request.RequestedForActorRole.Name switch
            {
                DataHubNames.ActorRole.EnergySupplier =>
                [
                    TimeSeriesType.Production,
                    TimeSeriesType.FlexConsumption,
                    TimeSeriesType.NonProfiledConsumption,
                ],
                DataHubNames.ActorRole.BalanceResponsibleParty =>
                [
                    TimeSeriesType.Production,
                    TimeSeriesType.FlexConsumption,
                    TimeSeriesType.NonProfiledConsumption,
                ],
                DataHubNames.ActorRole.MeteredDataResponsible =>
                [
                    TimeSeriesType.Production,
                    TimeSeriesType.FlexConsumption,
                    TimeSeriesType.NonProfiledConsumption,
                    TimeSeriesType.TotalConsumption,
                    TimeSeriesType.NetExchangePerGa,
                ],
                _ => throw new ArgumentOutOfRangeException(
                    nameof(request.RequestedForActorRole),
                    request.RequestedForActorRole,
                    "Value does not contain a valid string representation of a requested by actor role."),
            };
    }

    private static AggregationPerRoleAndGridArea MapAggregationPerRoleAndGridArea(RequestCalculatedEnergyTimeSeriesAcceptedV1 request)
    {
        return new AggregationPerRoleAndGridArea(
            GridAreaCodes: request.GridAreas,
            EnergySupplierId: request.EnergySupplierNumber?.Value,
            BalanceResponsibleId: request.BalanceResponsibleNumber?.Value);
    }
}
