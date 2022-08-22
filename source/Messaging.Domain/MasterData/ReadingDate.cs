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

using System.Text.Json.Serialization;

namespace Messaging.Domain.MasterData;

public class ReadingDate
{
    public ReadingDate()
    {
    }

    private ReadingDate(string? date)
    {
        Date = date;
    }

    public string? Date { get; init; }

    public static ReadingDate Create(string? monthDay)
    {
        if (monthDay == null) return new ReadingDate(monthDay);

        var formattedDate = FormatDateFromScheduledMeterReadingDate(monthDay);
        return new ReadingDate(formattedDate);
    }

    private static string FormatDateFromScheduledMeterReadingDate(string? monthDay)
    {
        return "--" + string.Concat(monthDay.AsSpan(0, 2).ToString())
                    + "-" +
                    string.Concat(monthDay.AsSpan(2).ToString());
    }
}
