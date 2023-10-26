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

using Energinet.DataHub.EDI.B2CWebApi.Factories;
using NodaTime;
using NodaTime.Text;
using Xunit;

namespace Energinet.DataHub.EDI.Tests.B2CWebApi.RequestAggregatedMeasureData;

public class InstantFormatFactoryTests
{
    private readonly DateTimeZone _dateTimeZone = DateTimeZoneProviders.Tzdb["Europe/Copenhagen"];

    [Theory]
    [InlineData("2023-10-02T22:00:00.000Z")]
    [InlineData("2023-10-07T21:59:59.999Z")]
    [InlineData("2022-06-23T22:00:00Z")]
    [InlineData("2022-06-23T21:00:00Z")]
    public void Can_set_instant_to_midnight(string instantString)
    {
        var instantAtMidget = InstantFormatFactory.SetInstantToMidnight(instantString, _dateTimeZone);
        var parseResult = InstantPattern.General.Parse(instantAtMidget);

        var zonedDateTime = new ZonedDateTime(parseResult.Value, _dateTimeZone);
        Assert.True(zonedDateTime.TimeOfDay == LocalTime.Midnight);
    }
}
