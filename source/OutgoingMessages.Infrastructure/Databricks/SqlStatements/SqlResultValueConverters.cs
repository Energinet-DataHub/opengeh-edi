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

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.SqlStatements;

public static class SqlResultValueConverters
{
    public static string ToNonEmptyString(this DatabricksSqlRow row, string columnName)
    {
        var value = row[columnName];
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return value;
    }

    public static Instant ToInstant(this DatabricksSqlRow row, string columnName)
    {
        var value = row[columnName];
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return InstantPattern.ExtendedIso.Parse(value).Value;
    }

    public static decimal ToDecimal(this DatabricksSqlRow row, string columnName)
    {
        var value = row[columnName];
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return decimal.Parse(value, CultureInfo.InvariantCulture);
    }

    public static DateTimeOffset ToDateTimeOffset(this DatabricksSqlRow row, string columnName)
    {
        var value = row[columnName];
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Parse from Databricks "INT" to int.
    /// </summary>
    public static int ToInt(this DatabricksSqlRow row, string columnName)
    {
        var value = row[columnName];
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return int.Parse(value);
    }

    /// <summary>
    /// Parse from Databricks "BIGINT" to long.
    /// </summary>
    public static long ToLong(this DatabricksSqlRow row, string columnName)
    {
        var value = row[columnName];
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return long.Parse(value);
    }

    public static Guid ToGuid(this DatabricksSqlRow row, string columnName)
    {
        var value = row[columnName];
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return Guid.Parse(value);
    }

    public static bool ToBool(this DatabricksSqlRow row, string columnName)
    {
        var value = row[columnName];

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
