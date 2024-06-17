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

using NodaTime;
using NodaTime.Text;

namespace Energinet.DataHub.EDI.B2CWebApi.Factories;

public static class InstantFormatFactory
{
    public static Instant SetInstantToMidnight(string dateString, DateTimeZone dateTimeZone, Duration? addDuration = null)
    {
        var instant = InstantPattern.ExtendedIso.Parse(dateString).Value;

        if (addDuration is not null)
        {
            instant = instant.Plus(addDuration.Value);
        }

        var zonedDateTime = new ZonedDateTime(instant, dateTimeZone);
        var dateTimeZoneAtMidnight = zonedDateTime.Date
            .At(LocalTime.Midnight);

        return dateTimeZoneAtMidnight
            .InZoneStrictly(dateTimeZone)
            .ToInstant();
    }
}
