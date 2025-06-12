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

namespace Energinet.DataHub.EDI.B2BApi.MeasurementsSynchronization;

public static class MeasurementsToMarketActivityRecordTransformer
{
    public static List<MeteredDataForMeteringPointMarketActivityRecord> Transform(Instant creationTime, List<TimeSeries> timeSeries)
    {
        var series = GetNonNullOriginalTransactionIdTimeSeries(timeSeries);
        var nonDeletedTimeSeries = GetNonDeletedTimeSeries(series);
        var validTimeSeries = GetNonCancelledTimeSeries(nonDeletedTimeSeries);

        var meteredDataForMeteringPointMarketActivityRecords = validTimeSeries.Select(ts =>
            {
                return new MeteredDataForMeteringPointMarketActivityRecord(
                    TransactionId.From(ts.OriginalTimeSeriesId!),
                    ts.AggregationCriteria.MeteringPointId,
                    MeteringPointType.FromCode(ts.TypeOfMP),
                    null,
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

    private static IEnumerable<TimeSeries> GetNonNullOriginalTransactionIdTimeSeries(IEnumerable<TimeSeries> timeSeries)
    {
        return timeSeries.Where(ts => ts.OriginalTimeSeriesId != null);
    }

    private static IEnumerable<TimeSeries> GetNonDeletedTimeSeries(IEnumerable<TimeSeries> timeSeries)
    {
        return timeSeries.Where(ts => ts.TimeSeriesStatus != "9");
    }

    private static IEnumerable<TimeSeries> GetNonCancelledTimeSeries(IEnumerable<TimeSeries> timeSeries)
    {
        return timeSeries.Where(x => x.Observation.Any(o => o.QuantityMissingIndicator is not true));
    }

    private static string TryGetQualityFromEbixCode(Observation obs)
    {
        var quantityMissingIndicator = obs.QuantityMissingIndicator ?? false;
        var fallbackQuality = quantityMissingIndicator ? Quality.NotAvailable.Name : "Invalid_Quality";
        return Quality.TryGetNameFromEbixCode(obs.QuantityQuality, fallbackQuality)!;
    }
}
