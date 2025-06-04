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

using System.Text.RegularExpressions;
using Energinet.DataHub.EDI.B2CWebApi.Factories.V1;
using Energinet.DataHub.EDI.B2CWebApi.Models.V1;
using NodaTime;
using Xunit;

namespace Energinet.DataHub.EDI.Tests.B2CWebApi.Factories;

public class RequestAggregatedMeasureDataDtoFactoryV1Test
{
    [Fact]
    public void CreateRequestAggregatedMeasureDataDto_ShouldReturnValidDto()
    {
        // Act
        var result = RequestAggregatedMeasureDataDtoFactoryV1.Create(
            new RequestAggregatedMeasureDataMarketRequestV1(
                BusinessReason.PreliminaryAggregation,
                null,
                SettlementMethod.NonProfiled,
                MeteringPointType.Consumption,
                new DateTimeOffset(2021, 1, 1, 22, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2021, 1, 31, 22, 0, 0, TimeSpan.Zero),
                "804",
                "6454843448453",
                null),
            "1234567895412",
            "EnergySupplier",
            Instant.FromUtc(2021, 1, 1, 22, 00));

        // Assert
        Assert.True(IsIsoUtcFormat(result.Serie.First().StartDateAndOrTimeDateTime));
        Assert.True(IsIsoUtcFormat(result.Serie.First().EndDateAndOrTimeDateTime!));
    }

    private bool IsIsoUtcFormat(string input)
    {
        return Regex.IsMatch(input, @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z$");
    }
}
