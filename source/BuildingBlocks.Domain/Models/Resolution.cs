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
using NodaTime;
using PMTypes = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects;

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

[Serializable]
public class Resolution : DataHubType<Resolution>
{
    public static readonly Resolution QuarterHourly = new(PMTypes.Resolution.QuarterHourly.Name, "PT15M");
    public static readonly Resolution Hourly = new(PMTypes.Resolution.Hourly.Name, "PT1H");
    public static readonly Resolution Daily = new(PMTypes.Resolution.Daily.Name, "P1D");
    public static readonly Resolution Monthly = new(PMTypes.Resolution.Monthly.Name, "P1M");

    [JsonConstructor]
    private Resolution(string name, string code)
        : base(name, code)
    {
    }

    public ProcessManager.Components.Abstractions.ValueObjects.Resolution ToProcessManagerResolution()
    {
        return ProcessManager.Components.Abstractions.ValueObjects.Resolution.FromName(Name);
    }

    public Duration ToDuration()
    {
        var resolutionDuration = this switch
        {
            var r when r == QuarterHourly => Duration.FromMinutes(15),
            var r when r == Hourly => Duration.FromHours(1),
            var r when r == Daily => Duration.FromDays(1),
            var r when r == Monthly => throw new InvalidOperationException("Monthly resolution to duration is not supported, since a month is not a fixed duration."),
            _ => throw new ArgumentOutOfRangeException(nameof(Name), Name, "Unknown resolution."),
        };

        return resolutionDuration;
    }
}
