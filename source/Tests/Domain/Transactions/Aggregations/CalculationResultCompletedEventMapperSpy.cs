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

using Application.Transactions.Aggregations;
using Energinet.DataHub.Wholesale.Contracts.Events;
using Infrastructure.Transactions.Aggregations;

namespace Tests.Domain.Transactions.Aggregations;

public class CalculationResultCompletedEventMapperSpy : CalculationResultCompletedEventMapper
{
    public CalculationResultCompletedEventMapperSpy(IGridAreaLookup gridAreaLookup)
        : base(gridAreaLookup)
    {
    }

    public static void MapProcessTypeSpy(ProcessType processType)
    {
        MapProcessType(processType);
    }

    public static void MapTimeSeriesTypeSpy(TimeSeriesType timeSeriesType)
    {
        MapMeteringPointType(timeSeriesType);
    }

    public static void MapResolutionSpy(Resolution resolution)
    {
        MapResolution(resolution);
    }

    public static void MapQuantityQualitySpy(QuantityQuality quantityQuality)
    {
        MapQuality(quantityQuality);
    }

    public static void MapQuantityUnitSpy(QuantityUnit quantityUnit)
    {
        MapUnitType(quantityUnit);
    }
}
