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

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Migration;

public static class Dh2TimeSeriesSyncConstants
{
    public const string MeteredDataTimeSeriesDh3 = "MeteredDataTimeSeriesDH3";
    public const string Header = "Header";
    public const string MessageId = "MessageId";
    public const string TimeSeries = "TimeSeries";
    public const string AggregationCriteria = "AggregationCriteria";
    public const string MeteringPointId = "MeteringPointId";
    public const string TypeOfMp = "TypeOfMP";
    public const string OriginalTimeSeriesId = "OriginalTimeSeriesId";
    public const string TransactionInsertDate = "TransactionInsertDate";
    public const string OriginalMessageId = "OriginalMessageId";
    public const string TimeSeriesPeriod = "TimeSeriesPeriod";
    public const string Start = "Start";
    public const string End = "End";
    public const string ResolutionDuration = "ResolutionDuration";
    public const string EnergyTimeSeriesMeasureUnit = "EnergyTimeSeriesMeasureUnit";
    public const string TimeSeriesStatus = "TimeSeriesStatus";
    public const string Observation = "Observation";
    public const string Position = "Position";
    public const string EnergyQuantity = "EnergyQuantity";
    public const string QuantityQuality = "QuantityQuality";
    public const string QuantityMissingIndicator = "QuantityMissingIndicator";
}
