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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults;
using Google.Protobuf.Collections;
using NodaTime;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.Asserts;

public record TimeSeriesPointAssertionInput(
    Instant Time,
    decimal Quantity,
    CalculatedQuantityQuality Quality)
{
    private static CalculatedQuantityQuality ConvertQuality(RepeatedField<QuantityQuality> qualities)
    {
        var expectedQuantityQuality = qualities.Single() switch
        {
            QuantityQuality.Calculated => CalculatedQuantityQuality.Calculated,
            QuantityQuality.Measured => CalculatedQuantityQuality.Measured,
            QuantityQuality.Estimated => CalculatedQuantityQuality.Estimated,
            QuantityQuality.Missing => CalculatedQuantityQuality.Missing,
            _ => throw new NotImplementedException(
                $"Quantity quality {qualities.Single()} not implemented"),
        };

        return expectedQuantityQuality;
    }
}
