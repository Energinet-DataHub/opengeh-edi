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

using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableMappers;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.SqlStatements;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Queries;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Factories;

public static class WholesaleTimeSeriesPointFactory
{
    public static WholesaleTimeSeriesPoint Create(DatabricksSqlRow databricksSqlRow)
    {
        return new WholesaleTimeSeriesPoint(
            databricksSqlRow.ToInstant(WholesaleResultColumnNames.Time),
            databricksSqlRow.ToNullableDecimal(WholesaleResultColumnNames.Quantity),
            QuantityQualitiesMapper.TryFromDeltaTableValue(databricksSqlRow.ToNullableString(WholesaleResultColumnNames.QuantityQualities)),
            databricksSqlRow.ToNullableDecimal(WholesaleResultColumnNames.Price),
            databricksSqlRow.ToNullableDecimal(WholesaleResultColumnNames.Amount));
    }
}
