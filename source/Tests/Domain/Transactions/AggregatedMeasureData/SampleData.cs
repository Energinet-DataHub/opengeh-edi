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

using System;
using NodaTime;

namespace Tests.Domain.Transactions.AggregatedMeasureData;

public static class SampleData
{
    internal static Guid ProcessId => Guid.Parse("17DE02FC-6A83-436F-BC89-779ABBD6AB35");

    internal static string BusinessTransactionId => "17DE02FC-6A83-436F-BC89-779ABBD6AB35";

    internal static string? SettlementVersion => null;

    internal static string? MeteringPointType => null;

    internal static string? SettlementMethod => null;

    internal static string StartOfPeriod => SystemClock.Instance.GetCurrentInstant().ToString();

    internal static string EndOfPeriod => SystemClock.Instance.GetCurrentInstant().ToString();

    internal static string? MeteringGridAreaDomainId => null;

    internal static string? EnergySupplierId => null;

    internal static string? BalanceResponsibleId => null;

    internal static string RequestedByActorId => "1234567890123";
}
