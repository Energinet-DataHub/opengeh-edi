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
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.Asserts;
using NodaTime;

namespace Energinet.DataHub.EDI.IntegrationTests.Factories;

internal static class TimeSeriesPointsFactory
{
    public static IReadOnlyCollection<TimeSeriesPointAssertionInput> CreatePointsForDay(
        Instant start,
        decimal quantity,
        CalculatedQuantityQuality calculatedQuality)
    {
        var points = new List<TimeSeriesPointAssertionInput>();
        for (var i = 0; i < 24; i++)
        {
            points.Add(new TimeSeriesPointAssertionInput(
                start.Plus(Duration.FromHours(i)),
                quantity,
                calculatedQuality));
        }

        return points;
    }
}