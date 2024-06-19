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
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Models;
using NodaTime;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults;

public sealed class WholesaleAmountPerCharge(
    Guid id,
    Guid calculationId,
    CalculationType calculationType,
    string gridAreaCode,
    string energySupplierId,
    string chargeOwnerId,
    long calculationVersion,
    Instant periodStartUtc,
    Instant periodEndUtc,
    MeasurementUnit quantityUnit,
    Currency currency,
    ChargeType? chargeType,
    Resolution resolution,
    MeteringPointType? meteringPointType,
    SettlementMethod? settlementMethod,
    IReadOnlyCollection<WholesaleTimeSeriesPoint> timeSeriesPoints,
    bool isTax,
    string? chargeCode) : WholesaleTimeSeries(
    calculationType,
    gridAreaCode,
    calculationVersion,
    periodStartUtc,
    periodEndUtc,
    quantityUnit,
    resolution,
    currency,
    energySupplierId)
{
    public Guid Id { get; } = id;

    public Guid CalculationId { get; } = calculationId;

    public string ChargeOwnerId { get; } = chargeOwnerId;

    public ChargeType? ChargeType { get; } = chargeType;

    public MeteringPointType? MeteringPointType { get; } = meteringPointType;

    public SettlementMethod? SettlementMethod { get; } = settlementMethod;

    public IReadOnlyCollection<WholesaleTimeSeriesPoint> TimeSeriesPoints { get; } = timeSeriesPoints;

    public bool IsTax { get; } = isTax;

    public string? ChargeCode { get; } = chargeCode;
}
