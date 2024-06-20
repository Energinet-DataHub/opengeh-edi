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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableConstants;
using NodaTime;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Models;

public abstract class WholesaleTimeSeries(
    CalculationType calculationType,
    string gridAreaCode,
    long calculationVersion,
    Instant periodStartUtc,
    Instant periodEndUtc,
    MeasurementUnit quantityUnit,
    Resolution resolution,
    Currency currency,
    string energySupplierId)
{
    public CalculationType CalculationType { get; } = calculationType;

    public string GridAreaCode { get; } = gridAreaCode;

    public long CalculationVersion { get; } = calculationVersion;

    public Instant PeriodStartUtc { get; } = periodStartUtc;

    public Instant PeriodEndUtc { get; } = periodEndUtc;

    public MeasurementUnit QuantityUnit { get; } = quantityUnit;

    public Resolution Resolution { get; } = resolution;

    public Currency Currency { get; } = currency;

    public string EnergySupplierId { get; } = energySupplierId;
}
