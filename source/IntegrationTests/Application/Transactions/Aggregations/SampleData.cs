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

using System;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IntegrationTests.Factories;
using NodaTime;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.Transactions.Aggregations;

internal sealed class SampleData
{
    internal static ActorNumber GridOperatorNumber => ActorNumber.Create("8200000007739");

    internal static string GridAreaCode => "805";

    internal static Guid ResultId => Guid.Parse("42AB7292-FE2E-4F33-B537-4A15FEDB9754");

    internal static MeteringPointType MeteringPointType => MeteringPointType.Production;

    internal static MeasurementUnit MeasureUnitType => MeasurementUnit.Kwh;

    internal static Resolution Resolution => Resolution.Hourly;

    internal static Instant StartOfPeriod => EffectiveDateFactory.InstantAsOfToday();

    internal static Instant EndOfPeriod => EffectiveDateFactory.OffsetDaysFromToday(1);

    internal static ActorNumber EnergySupplierNumber => ActorNumber.Create("8200000007740");

    internal static ActorNumber EnergySupplierNumber2 => ActorNumber.Create("8200000007742");

    internal static ActorNumber BalanceResponsibleNumber => ActorNumber.Create("8200000007743");
}
