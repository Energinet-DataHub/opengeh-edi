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
using NodaTime;
using NodaTime.Text;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.EnergyResults.SqlStatements;

public static class SqlResultValueConverters
{
    public static Instant ToInstant(string value)
    {
        return InstantPattern.ExtendedIso.Parse(value).Value;
    }

    public static decimal ToDecimal(string value)
    {
        return decimal.Parse(value, CultureInfo.InvariantCulture);
    }

    public static DateTimeOffset ToDateTimeOffset(string value)
    {
        return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture);
    }

    public static int ToInt(string value)
    {
        return int.Parse(value);
    }

    public static Guid ToGuid(string value)
    {
        return Guid.Parse(value);
    }

    public static bool ToBool(string value)
    {
        return value switch
        {
            "true" => true,
            "false" => false,

            _ => throw new ArgumentOutOfRangeException(
                nameof(value),
                actualValue: value,
                "Value does not contain a valid string representation of a boolean."),
        };
    }
}
