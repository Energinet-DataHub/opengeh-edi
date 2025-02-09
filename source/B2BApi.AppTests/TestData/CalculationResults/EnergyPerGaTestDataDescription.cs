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

using Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects;
using NodaTime;

namespace Energinet.DataHub.EDI.B2BApi.AppTests.TestData.CalculationResults;

public class EnergyPerGaTestDataDescription
{
    public static readonly Instant PeriodStart = Instant.FromUtc(2022, 01, 01, 23, 00);
    public static readonly Instant PeriodEnd = Instant.FromUtc(2022, 01, 04, 23, 00);

    public static readonly ResultSet ResultSet1 = new(
        PeriodStart: PeriodStart,
        PeriodEnd: PeriodEnd,
        GridArea: "804",
        BusinessReason: BusinessReason.BalanceFixing,
        MeteringPointType: MeteringPointType.Consumption,
        ExpectedMessagesCount: 2);

    public record ResultSet(
        Instant PeriodStart,
        Instant PeriodEnd,
        string GridArea,
        BusinessReason BusinessReason,
        MeteringPointType MeteringPointType,
        int ExpectedMessagesCount);
}
