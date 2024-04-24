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
using System.Collections.Generic;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;

public record AcceptedWholesaleServicesSeries(
    Guid TransactionId,
    long CalculationVersion,
    string GridAreaCode,
    string ChargeCode,
    bool IsTax,
    IReadOnlyCollection<WholesaleServicesPoint> Points,
    ActorNumber EnergySupplier,
    ActorNumber ChargeOwner,
    Period Period,
    SettlementVersion? SettlementVersion,
    MeasurementUnit QuantityMeasureUnit,
    MeasurementUnit PriceMeasureUnit,
    Currency Currency,
    ChargeType ChargeType,
    Resolution Resolution,
    MeteringPointType? MeteringPointType,
    SettlementMethod? SettlementMethod,
    string OriginalTransactionIdReference) : WholesaleServicesSeries(
    TransactionId,
    CalculationVersion,
    GridAreaCode,
    ChargeCode,
    IsTax,
    Points,
    EnergySupplier,
    ChargeOwner,
    Period,
    SettlementVersion,
    QuantityMeasureUnit,
    null,
    PriceMeasureUnit,
    Currency,
    ChargeType,
    Resolution,
    MeteringPointType,
    null,
    SettlementMethod,
    OriginalTransactionIdReference);
