﻿// Copyright 2020 Energinet DataHub A/S
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

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableMappers;

public static class ResolutionMapper
{
    public static Resolution FromDeltaTableValue(string resolution) =>
        resolution switch
        {
            "PT15M" => Resolution.QuarterHourly,
            "PT1H" => Resolution.Hourly,
            _ => throw new ArgumentOutOfRangeException(
                nameof(resolution),
                actualValue: resolution,
                "Value does not contain a valid string representation of a resolution."),
        };

    public static string ToDeltaTableValue(Resolution resolution)
    {
        string resolutionToReturn;
        switch (resolution)
        {
            case Resolution res when res == Resolution.QuarterHourly:
                {
                    resolutionToReturn = "PT15M";
                    break;
                }

            case Resolution res when res == Resolution.Hourly:
                {
                    resolutionToReturn = "PT1H";
                    break;
                }

            default:
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(resolution),
                        actualValue: resolution,
                        $"Cannot map ${nameof(Resolution)} to delta table value");
                }
        }

        return resolutionToReturn;
    }
}