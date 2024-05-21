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

using System.Linq;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.Factories.Mappers;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using FluentAssertions;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Xunit;
using DecimalValue = Energinet.DataHub.Wholesale.Contracts.IntegrationEvents.Common.DecimalValue;

namespace Energinet.DataHub.EDI.Tests.Application.Process.Transactions.Mappers;

public class PointsMapperTests
{
    [Fact]
    public void Ensure_energy_result_produced_v2_time_series_points_is_mapped()
    {
        // Arrange
        var protoPoint = new EnergyResultProducedV2.Types.TimeSeriesPoint()
        {
            Time = new Timestamp { Seconds = 100000 },
            Quantity = new DecimalValue { Units = 123, Nanos = 1200000 },
            QuantityQualities = { EnergyResultProducedV2.Types.QuantityQuality.Calculated },
        };

        // Act
        var actual = PointsMapper
            .Map(new RepeatedField<EnergyResultProducedV2.Types.TimeSeriesPoint>() { protoPoint });

        // Assert
        actual.Should().ContainSingle();
        actual.First().Quantity.Should().Be(protoPoint.Quantity.Units + (protoPoint.Quantity.Nanos / 1_000_000_000M));
        actual.First().Position.Should().BeGreaterThan(0);
        actual.First().QuantityQuality.Should().Be(CalculatedQuantityQuality.Calculated);
        actual.First().SampleTime.Should().Be(protoPoint.Time.ToString());
    }

    [Fact]
    public void Ensure_amount_per_charge_result_produced_v1_time_series_points_is_mapped()
    {
        // Arrange
        var protoPoint = new AmountPerChargeResultProducedV1.Types.TimeSeriesPoint()
        {
            Time = new Timestamp { Seconds = 100000 },
            Quantity = new DecimalValue { Units = 123, Nanos = 1200000 },
            QuantityQualities = { AmountPerChargeResultProducedV1.Types.QuantityQuality.Calculated },
            Price = new DecimalValue { Units = 122, Nanos = 1200000 },
        };

        // Act
        var actual = PointsMapper
            .Map(new RepeatedField<AmountPerChargeResultProducedV1.Types.TimeSeriesPoint>() { protoPoint }, AmountPerChargeResultProducedV1.Types.ChargeType.Tariff);

        // Assert
        actual.Should().ContainSingle();
        actual.First().Quantity.Should().Be(protoPoint.Quantity.Units + (protoPoint.Quantity.Nanos / 1_000_000_000M));
        actual.First().Position.Should().BeGreaterThan(0);
        actual.First().QuantityQuality.Should().Be(CalculatedQuantityQuality.Calculated);
    }
}
