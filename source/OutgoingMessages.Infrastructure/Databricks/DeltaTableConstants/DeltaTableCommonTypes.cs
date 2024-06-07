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

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableConstants;

public static class DeltaTableCommonTypes
{
    public const string String = "STRING";

    public const string Timestamp = "TIMESTAMP";

    /// <summary>
    /// Int or Int32 in C#
    /// </summary>
    public const string Int = "INT";

    /// <summary>
    /// Long or Int64 in C#
    /// </summary>
    public const string BigInt = "BIGINT";

    public const string Decimal18x3 = "DECIMAL(18, 3)";

    public const string ArrayOfStrings = "ARRAY<STRING>";
}
