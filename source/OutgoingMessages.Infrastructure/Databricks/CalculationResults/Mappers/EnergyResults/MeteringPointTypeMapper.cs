﻿// Copyright 2020 Energinet DataHub A/S
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
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults.DeltaTableConstants;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults.Mappers.EnergyResults;

public static class MeteringPointTypeMapper
{
    public static MeteringPointType FromDeltaTableValue(string meteringPointType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(meteringPointType);

        return meteringPointType switch
        {
            DeltaTableMeteringPointType.Consumption => MeteringPointType.Consumption,
            DeltaTableMeteringPointType.Production => MeteringPointType.Production,
            DeltaTableMeteringPointType.Exchange => MeteringPointType.Exchange,
            _ => throw new ArgumentOutOfRangeException(
                nameof(meteringPointType),
                actualValue: meteringPointType,
                "Value does not contain a valid string representation of a metering point type."),
        };
    }

    public static string? ToDeltaTableValue(MeteringPointType? meteringPointType)
    {
        return meteringPointType switch
        {
            var mp when mp == MeteringPointType.Consumption => DeltaTableMeteringPointType.Consumption,
            var mp when mp == MeteringPointType.Production => DeltaTableMeteringPointType.Production,
            var mp when mp == MeteringPointType.Exchange => DeltaTableMeteringPointType.Exchange,
            null => null,

            _ => throw new ArgumentOutOfRangeException(
                nameof(meteringPointType),
                meteringPointType,
                null),
        };
    }
}
