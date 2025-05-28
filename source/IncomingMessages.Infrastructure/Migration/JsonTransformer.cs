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

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Migration;

public class JsonTransformer : IJsonTransformer
{
    public List<TimeSeriesJsonPayload> TransformJsonMessage(string messageId, string jsonPayload)
    {
        using var document = JsonDocument.Parse(jsonPayload);
        var sourceObject = document.RootElement;

        var timeSeries = sourceObject
            .GetProperty(Dh2TimeSeriesSyncConstants.MeteredDataTimeSeriesDh3)
            .GetProperty(Dh2TimeSeriesSyncConstants.TimeSeries);
        var timeSeries2 = sourceObject
            .GetProperty("MeteredDataTimeSeriesDH3");

        if (timeSeries.ValueKind == JsonValueKind.Null)
        {
            throw new InvalidOperationException("MeteredDataTimeSeriesDH3 is missing in the message");
        }

        return CreateTimeSeriesJsonPayloads(messageId, timeSeries);
    }

    private static (JsonObject JsonObject, string MeteringPointId) CreateMeteringPointTarget(JsonElement timeSeries)
    {
        var meteringPointTarget = new JsonObject();

        var meteringPointId = timeSeries
            .GetProperty(Dh2TimeSeriesSyncConstants.AggregationCriteria)
            .GetProperty(Dh2TimeSeriesSyncConstants.MeteringPointId)
            .ToString();

        var masterData = new JsonArray();
        var masterDataItem = new JsonObject
        {
            { Dh3TimeSeriesSyncConstant.GridArea, "000" }, // TODO: LRN, is this enriched from somewhere else?
            { Dh3TimeSeriesSyncConstant.TypeOfMp, timeSeries.GetProperty(Dh2TimeSeriesSyncConstants.TypeOfMp).ToString() },
            { Dh3TimeSeriesSyncConstant.MasterDataStartDate, DateTime.UnixEpoch.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture) },
            { Dh3TimeSeriesSyncConstant.MasterDataEndDate, null },
        };
        masterData.Add(masterDataItem);

        meteringPointTarget.Add(Dh3TimeSeriesSyncConstant.MeteringPointId, meteringPointId);
        meteringPointTarget.Add(Dh3TimeSeriesSyncConstant.MasterData, masterData);

        return (meteringPointTarget, meteringPointId);
    }

    private static string GetOriginalTimeSeriesId(JsonElement timeSeries, int migrationTimeSeriesId)
    {
        var isOriginalTimeSeriesIdPresent = timeSeries.TryGetProperty(Dh2TimeSeriesSyncConstants.OriginalTimeSeriesId, out var originalTimeSeriesId);
        return !isOriginalTimeSeriesIdPresent || originalTimeSeriesId.ValueKind.Equals(JsonValueKind.Null)
            ? $"mig-{migrationTimeSeriesId:D8}"
            : originalTimeSeriesId.ToString();
    }

    private static string GetOriginalMessageId(JsonElement timeSeries, string messageId)
    {
        var isOriginalMessageIdPresent = timeSeries.TryGetProperty(Dh2TimeSeriesSyncConstants.OriginalMessageId, out var originalMessageId);
        return !isOriginalMessageIdPresent || originalMessageId.ValueKind.Equals(JsonValueKind.Null)
            ? $"mig-{messageId}"
            : originalMessageId.ToString();
    }

    private static int GetTimeSeriesStatus(JsonElement timeSeries)
    {
        var isTimeSeriesStatusPresent = timeSeries.TryGetProperty(Dh2TimeSeriesSyncConstants.TimeSeriesStatus, out var timeSeriesStatus);
        return !isTimeSeriesStatusPresent || timeSeriesStatus.ValueKind.Equals(JsonValueKind.Null)
            ? 2
            : Convert.ToInt32(timeSeriesStatus.ToString());
    }

    private static (JsonArray TimeSeriesValues, bool QuantityMissingForAllObservations) CreateTimeSeriesValues(JsonElement timeSeries)
    {
        var timeSeriesValues = new JsonArray();
        var quantityMissingForAllObservations = true;

        var observations = timeSeries.GetProperty(Dh2TimeSeriesSyncConstants.Observation);
        if (observations.ValueKind == JsonValueKind.Null)
        {
            return (timeSeriesValues, false);
        }

        foreach (var observation in observations.EnumerateArray())
        {
            quantityMissingForAllObservations = GetQuantityMissingIndicator(observation) && quantityMissingForAllObservations;
            var observationItem = new JsonObject
            {
                { Dh3TimeSeriesSyncConstant.Position, Convert.ToInt32(observation.GetProperty(Dh2TimeSeriesSyncConstants.Position).ToString()) },
                { Dh3TimeSeriesSyncConstant.Quantity, GetEnergyQuantity(observation) },
                { Dh3TimeSeriesSyncConstant.Quality, GetQuantityQuality(observation) },
            };
            timeSeriesValues.Add(observationItem);
        }

        return (timeSeriesValues, quantityMissingForAllObservations);
    }

    private static string GetQuantityQuality(JsonElement observation)
    {
        var quantityMissingIndicator = GetQuantityMissingIndicator(observation);
        observation.TryGetProperty(Dh2TimeSeriesSyncConstants.QuantityQuality, out var quality);
        return quantityMissingIndicator || quality.ValueKind.Equals(JsonValueKind.Null)
            ? "QM" // TODO: LRN what QQ is this? Proberly QualityMissing?
            : observation.GetProperty(Dh2TimeSeriesSyncConstants.QuantityQuality).ToString();
    }

    private static double GetEnergyQuantity(JsonElement observation)
    {
        var quantityMissingIndicator = GetQuantityMissingIndicator(observation);
        var isQuantityPresent = observation.TryGetProperty(Dh2TimeSeriesSyncConstants.EnergyQuantity, out var quantity);
        return quantityMissingIndicator && !isQuantityPresent
            ? 0 // TODO: LRN is nullable not supported?
            : Convert.ToDouble(quantity.ToString(), CultureInfo.InvariantCulture);
    }

    private static bool GetQuantityMissingIndicator(JsonElement observation)
    {
        return observation.TryGetProperty(Dh2TimeSeriesSyncConstants.QuantityMissingIndicator, out var missingIndicator)
               && missingIndicator.GetBoolean();
    }

    private List<TimeSeriesJsonPayload> CreateTimeSeriesJsonPayloads(string messageId, JsonElement timeSeries)
    {
        var migrationTimeSeriesId = 0;
        var result = new List<TimeSeriesJsonPayload>();
        foreach (var ts in timeSeries.EnumerateArray())
        {
            migrationTimeSeriesId++;
            var targetObject = new JsonObject();
            var (meteringPointTarget, meteringPointId) = CreateMeteringPointTarget(ts);
            var (timeSeriesTarget, timeSeriesId, transactionInsertDate) = CreateTimeSeriesTarget(ts, messageId, migrationTimeSeriesId);

            targetObject.Add(Dh3TimeSeriesSyncConstant.MeteringPoint, meteringPointTarget);
            targetObject.Add(Dh3TimeSeriesSyncConstant.TimeSeries, timeSeriesTarget);

            result.Add(new TimeSeriesJsonPayload(
                messageId,
                meteringPointId,
                timeSeriesId,
                transactionInsertDate,
                targetObject.ToString()));
        }

        return result;
    }

    private (JsonArray TimeSeriesArray, string TransactionId, string TransactionInsertDate) CreateTimeSeriesTarget(JsonElement timeSeries, string messageId, int migrationTimeSeriesId)
    {
        var timeSeriesTarget = new JsonArray();
        var transactionId = GetOriginalTimeSeriesId(timeSeries, migrationTimeSeriesId);
        var transactionInsertDate = timeSeries.GetProperty(Dh2TimeSeriesSyncConstants.TransactionInsertDate).ToString();
        var timeSeriesValuesResult = CreateTimeSeriesValues(timeSeries);
        var seriesItem = new JsonObject
        {
            { Dh3TimeSeriesSyncConstant.TransactionId, transactionId },
            { Dh3TimeSeriesSyncConstant.MessageId, GetOriginalMessageId(timeSeries, messageId) },
            { Dh3TimeSeriesSyncConstant.ValidFromDate, timeSeries.GetProperty(Dh2TimeSeriesSyncConstants.TimeSeriesPeriod).GetProperty(Dh2TimeSeriesSyncConstants.Start).ToString() },
            { Dh3TimeSeriesSyncConstant.ValidToDate, timeSeries.GetProperty(Dh2TimeSeriesSyncConstants.TimeSeriesPeriod).GetProperty(Dh2TimeSeriesSyncConstants.End).ToString() },
            { Dh3TimeSeriesSyncConstant.TransactionInsertDate, transactionInsertDate },
            { Dh3TimeSeriesSyncConstant.HistoricalFlag, timeSeriesValuesResult.QuantityMissingForAllObservations ? "Y" : "N" },
            { Dh3TimeSeriesSyncConstant.Resolution, timeSeries.GetProperty(Dh2TimeSeriesSyncConstants.TimeSeriesPeriod).GetProperty(Dh2TimeSeriesSyncConstants.ResolutionDuration).ToString() },
            { Dh3TimeSeriesSyncConstant.Unit, timeSeries.GetProperty(Dh2TimeSeriesSyncConstants.EnergyTimeSeriesMeasureUnit).ToString() },
            { Dh3TimeSeriesSyncConstant.Status, GetTimeSeriesStatus(timeSeries) },
            { Dh3TimeSeriesSyncConstant.ReadReason, timeSeriesValuesResult.QuantityMissingForAllObservations ? "CAN" : string.Empty },
            { Dh3TimeSeriesSyncConstant.Values, timeSeriesValuesResult.TimeSeriesValues },
        };
        timeSeriesTarget.Add(seriesItem);

        return (timeSeriesTarget, transactionId, transactionInsertDate);
    }
}
