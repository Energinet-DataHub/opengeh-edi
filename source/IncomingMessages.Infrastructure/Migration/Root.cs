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

using System.Text.Json.Serialization;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Migration;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
public class Root
{
    [JsonPropertyName("MeteredDataTimeSeriesDH3")]
    public MeteredDataTimeSeriesDH3 MeteredDataTimeSeriesDH3 { get; set; }
}

public class MeteredDataTimeSeriesDH3
{
    public Header Header { get; set; }

    public List<TimeSeries> TimeSeries { get; set; }
}

public class Header
{
    public string MessageId { get; set; }

    public string DocumentType { get; set; }

    public DateTimeOffset Creation { get; set; }

    public string EnergyBusinessProcess { get; set; }

    public string EnergyIndustryClassification { get; set; }

    public Identification SenderIdentification { get; set; }

    public Identification RecipientIdentification { get; set; }

    public string EnergyBusinessProcessRole { get; set; }
}

public class Identification
{
    public string SchemeAgencyIdentifier { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; }
}

public class TimeSeries
{
    public string TimeSeriesId { get; set; }

    public string OriginalMessageId { get; set; }

    public string OriginalTimeSeriesId { get; set; }

    public string EnergyTimeSeriesFunction { get; set; }

    public string EnergyTimeSeriesProduct { get; set; }

    public string EnergyTimeSeriesMeasureUnit { get; set; }

    public string TypeOfMP { get; set; }

    public string SettlementMethod { get; set; }

    public AggregationCriteria AggregationCriteria { get; set; }

    public List<Observation> Observation { get; set; }

    public TimeSeriesPeriod TimeSeriesPeriod { get; set; }

    public DateTime TransactionInsertDate { get; set; }

    public string TimeSeriesStatus { get; set; }
}

public class AggregationCriteria
{
    public string MeteringPointId { get; set; }
}

public class Observation
{
    public int Position { get; set; }

    public string QuantityQuality { get; set; }

    public decimal EnergyQuantity { get; set; }
}

public class TimeSeriesPeriod
{
    public string ResolutionDuration { get; set; }

    public DateTimeOffset Start { get; set; }

    public DateTimeOffset End { get; set; }
}
