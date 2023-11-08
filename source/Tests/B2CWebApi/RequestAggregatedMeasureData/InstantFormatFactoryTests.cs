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
using Microsoft.EntityFrameworkCore.SqlServer.NodaTime.Extensions;
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
    [InlineData("2022-01-23T23:00:00Z")]
    public void Can_set_instant_to_midnight(string instantString)
    {
        var instantAtMidget = InstantFormatFactory.SetInstantToMidnight(instantString, _dateTimeZone);

        var zonedDateTime = new ZonedDateTime(instantAtMidget, _dateTimeZone);
        Assert.True(zonedDateTime.TimeOfDay == LocalTime.Midnight);
    }

    [Theory]
    [InlineData("2023-10-02T22:00:00.000Z")]
    [InlineData("2022-06-23T22:00:00Z")]
    [InlineData("2022-01-23T23:00:00Z")]
    public void Converts_to_same_day(string instantString)
    {
        var instantAtMidget = InstantFormatFactory.SetInstantToMidnight(instantString, _dateTimeZone);

        var inputInstant = InstantPattern.ExtendedIso.Parse(instantString).GetValueOrThrow();
        Assert.True(instantAtMidget.Day() == inputInstant.Day(), $"The inputData was: {inputInstant} and the output was: {instantAtMidget}");
    }

    [Theory]
    // summer time
    [InlineData("2023-10-07T21:59:59.999Z", 1)]
    [InlineData("2022-06-23T21:00:00Z", 3600000)] // adding an hour
    [InlineData("2023-10-07T21:59:59.999Z", 3600000)] // adding an hour
    // Winter time
    [InlineData("2023-10-01T22:59:59.999Z", 1)]
    [InlineData("2022-06-22T22:00:00Z", 3600000)] // adding an hour
    public void Converts_to_next_day(string instantString, int milliseconds)
    {
        var instantAtMidget = InstantFormatFactory.SetInstantToMidnight(instantString, _dateTimeZone, Duration.FromMilliseconds(milliseconds));

        var inputInstant = InstantPattern.ExtendedIso.Parse(instantString).GetValueOrThrow();
        Assert.True(instantAtMidget.Day() == inputInstant.Day(), $"The inputData was: {inputInstant} and the output was: {instantAtMidget}");
    }
}
