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

namespace Messaging.Domain.OutgoingMessages.NotifyAggregatedMeasureData;

public record TimeSeries(Guid Id, string GridAreaCode, string MeteringPointType, string MeasureUnitType, Period Period);

public record Period(string Resolution, TimeInterval TimeInterval, IReadOnlyList<Point> Point);
public record TimeInterval(string Start, string End);
public record Point(int Position, decimal? Quantity, string? Quality);
