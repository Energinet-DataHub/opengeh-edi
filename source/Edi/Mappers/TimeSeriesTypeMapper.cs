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

using Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects;
using Energinet.DataHub.Wholesale.Edi.Models;

namespace Energinet.DataHub.Wholesale.Edi.Mappers;

public static class TimeSeriesTypeMapper
{
    public static TimeSeriesType MapTimeSeriesType(string meteringPointTypeName, string? settlementMethodName)
    {
        return meteringPointTypeName switch
        {
            var mpt when mpt == MeteringPointType.Production.Name => TimeSeriesType.Production,
            var mpt when mpt == MeteringPointType.Exchange.Name => TimeSeriesType.NetExchangePerGa,
            var mpt when mpt == MeteringPointType.Consumption.Name => settlementMethodName switch
            {
                var sm when string.IsNullOrWhiteSpace(sm) => TimeSeriesType.TotalConsumption,
                var sm when sm == SettlementMethod.NonProfiled.Name => TimeSeriesType.NonProfiledConsumption,
                var sm when sm == SettlementMethod.Flex.Name => TimeSeriesType.FlexConsumption,

                _ => throw new ArgumentOutOfRangeException(
                    nameof(settlementMethodName),
                    actualValue: settlementMethodName,
                    "Value does not contain a valid string representation of a settlement method."),
            },

            _ => throw new ArgumentOutOfRangeException(
                nameof(meteringPointTypeName),
                actualValue: meteringPointTypeName,
                "Value does not contain a valid string representation of a metering point type."),
        };
    }
}
