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

using System.Globalization;
using NodaTime;

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

public record Period(Instant Start, Instant End)
{
    public string StartToString()
    {
        return ParsePeriodDateFrom(Start);
    }

    public string EndToString()
    {
        return ParsePeriodDateFrom(End);
    }

    public string StartToEbixString()
    {
        return ParsePeriodDateFromToEbix(Start);
    }

    public string EndToEbixString()
    {
        return ParsePeriodDateFromToEbix(End);
    }

    private static string ParsePeriodDateFromToEbix(Instant instant)
    {
        return instant.ToString("yyyy-MM-ddTHH:mm:ss'Z'", CultureInfo.InvariantCulture);
    }

    private static string ParsePeriodDateFrom(Instant instant)
    {
        return instant.ToString("yyyy-MM-ddTHH:mm'Z'", CultureInfo.InvariantCulture);
    }
}
