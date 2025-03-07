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

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults.Mappers.WholesaleResults;

public static class ResolutionMapper
{
    public static Resolution FromDeltaTableValue(string resolution) =>
        resolution switch
        {
            "P1M" => Resolution.Monthly,
            "P1D" => Resolution.Daily,
            "PT1H" => Resolution.Hourly,
            _ => throw new ArgumentOutOfRangeException(
                nameof(resolution),
                actualValue: resolution,
                "Value does not contain a valid string representation of a resolution."),
        };

    public static string ToDeltaTableValue(Resolution resolution) =>
        resolution switch
        {
            var res when res == Resolution.Monthly => "P1M",
            var res when res == Resolution.Daily => "P1D",
            var res when res == Resolution.Hourly => "PT1H",
            _ => throw new ArgumentOutOfRangeException(
                nameof(resolution),
                actualValue: resolution,
                $"Cannot map ${nameof(Resolution)} to delta table value"),
        };
}
