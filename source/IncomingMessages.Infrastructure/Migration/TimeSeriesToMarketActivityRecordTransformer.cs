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
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.RSM012;
using NodaTime;
using NodaTime.Extensions;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Migration;

public class TimeSeriesToMarketActivityRecordTransformer : ITimeSeriesToMarketActivityRecordTransformer
{
    public List<MeteredDataForMeteringPointMarketActivityRecord> Transform(Instant creationTime, List<TimeSeries> timeSeries)
    {
        var internallyGeneratedId = 0;
        var meteredDataForMeteringPointMarketActivityRecords = timeSeries.Select(ts =>
            {
                internallyGeneratedId++;
                return new MeteredDataForMeteringPointMarketActivityRecord(
                    TransactionId.From(GetOriginalTimeSeriesId(ts.OriginalTimeSeriesId, internallyGeneratedId)),
                    ts.AggregationCriteria.MeteringPointId,
                    MeteringPointType.FromCode(ts.TypeOfMP),
                    null, // TODO: LRN, is this right?
                    ts.EnergyTimeSeriesProduct,
                    MeasurementUnit.FromCode(ts.EnergyTimeSeriesMeasureUnit),
                    creationTime,
                    Resolution.FromCode(ts.TimeSeriesPeriod.ResolutionDuration),
                    new Period(ts.TimeSeriesPeriod.Start.ToInstant(), ts.TimeSeriesPeriod.End.ToInstant()),
                    ts.Observation.Select(
                            obs => new PointActivityRecord(
                                obs.Position,
                                Quality.FromName(TryGetQualityFromEbixCode(obs)),
                                obs.EnergyQuantity))
                        .ToList());
            })
            .ToList();

        return meteredDataForMeteringPointMarketActivityRecords;
    }

    private static string GetOriginalTimeSeriesId(string? originalTimeSeriesId, int internallyGeneratedId)
    {
        return originalTimeSeriesId ?? $"mig-{internallyGeneratedId:D8}";
    }

    private static string TryGetQualityFromEbixCode(Observation obs)
    {
        var quantityMissingIndicator = obs.QuantityMissingIndicator ?? false;
        var fallbackQuality = quantityMissingIndicator ? Quality.NotAvailable.Name : "Invalid_Quality";
        return Quality.TryGetNameFromEbixCode(obs.QuantityQuality, fallbackQuality)!;
    }
}
