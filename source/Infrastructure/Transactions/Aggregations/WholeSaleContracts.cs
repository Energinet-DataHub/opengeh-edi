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

namespace Infrastructure.Transactions.Aggregations;

internal static class WholeSaleContracts
{
    internal enum ProcessStepType
    {
        AggregateProductionPerGridArea = 25,
    }

    internal enum TimeSeriesType
    {
        NonProfiledConsumption = 1,
        FlexConsumption = 2,
        Production = 3,
    }

    internal enum MarketRole
    {
        EnergySupplier = 0,
    }

    internal sealed record ProcessStepResultRequestDto(Guid BatchId, string GridAreaCode, WholeSaleContracts.ProcessStepType ProcessStepResult);

    internal sealed record ProcessStepResultRequestDtoV2(Guid BatchId, string GridAreaCode, TimeSeriesType TimeSeriesType, string Gln);

    internal sealed record ProcessStepActorsRequest(Guid BatchId, string GridAreaCode, WholeSaleContracts.TimeSeriesType Type, MarketRole MarketRole);

    internal sealed record WholesaleActorDto(string Gln);
}
