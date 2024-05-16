// // Copyright 2020 Energinet DataHub A/S
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
using System.Collections.Generic;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.NotifyWholesaleServices;

// This is a copy of WholesaleCalculationSeries defined in the process module
public record WholesaleCalculationMarketActivityRecord(
    Guid TransactionId,
    long CalculationVersion,
    string GridAreaCode,
    string? ChargeCode,
    bool IsTax,
    IReadOnlyCollection<Point> Points,
    ActorNumber EnergySupplier,
    ActorNumber? ChargeOwner,
    Period Period,
    SettlementVersion? SettlementVersion,
    MeasurementUnit QuantityMeasureUnit,

    // TODO: Should be removed at a later time, when all old outgoing messages are peeked
    [property: Obsolete("Only kept for backwards compatibility, use QuantityMeasureUnit instead")]
    MeasurementUnit? QuantityUnit,

    MeasurementUnit? PriceMeasureUnit,
    Currency Currency,
    ChargeType? ChargeType,
    Resolution Resolution,
    MeteringPointType? MeteringPointType,

    // TODO: Should be removed at a later time, when all old outgoing messages are peeked
    [property: Obsolete("Only kept for backwards compatibility, use SettlementMethod instead")]
    SettlementMethod? SettlementType,

    SettlementMethod? SettlementMethod,
    string? OriginalTransactionIdReference);

public record Point(int Position, decimal? Quantity, decimal? Price, decimal? Amount, CalculatedQuantityQuality? QuantityQuality);
