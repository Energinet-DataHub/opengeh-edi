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
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults.Mappers;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults.Mappers.EnergyResults;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults.EnergyResults;
using NodaTime;
using NodaTime.Text;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure;

public static class SqlResultValueConverters
{
    public static Instant? ToInstant(string? value)
    {
        if (value == null)
            return null;
        return InstantPattern.ExtendedIso.Parse(value).Value;
    }

    public static int? ToInt(string? value)
    {
        if (value == null)
            return null;
        return int.Parse(value, CultureInfo.InvariantCulture);
    }

    public static decimal? ToDecimal(string? value)
    {
        if (value == null)
            return null;
        return decimal.Parse(value, CultureInfo.InvariantCulture);
    }

    public static DateTimeOffset? ToDateTimeOffset(string? value)
    {
        if (value == null)
            return null;
        return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture);
    }

    public static IReadOnlyCollection<QuantityQuality>? ToQuantityQualities(string? value)
    {
        return QuantityQualitiesMapper.FromDeltaTableValue(value);
    }

    public static TimeSeriesType ToTimeSeriesType(string value)
    {
        return Databricks.CalculationResults.Mappers.EnergyResults.TimeSeriesTypeMapper.FromDeltaTableValue(value);
    }

    public static Guid ToGuid(string value)
    {
        return Guid.Parse(value);
    }
}
