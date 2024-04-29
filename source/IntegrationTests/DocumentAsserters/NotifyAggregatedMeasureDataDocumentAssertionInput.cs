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

using System.Collections.Generic;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.Edi.Responses;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;
using Resolution = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Resolution;

namespace Energinet.DataHub.EDI.IntegrationTests.DocumentAsserters;

public record NotifyAggregatedMeasureDataDocumentAssertionInput(
    BusinessReasonWithSettlementVersion BusinessReasonWithSettlementVersion,
    ActorNumber SenderId,
    ActorNumber ReceiverId,
    string Timestamp,
    long CalculationVersion,
    MeteringPointType MeteringPointType,
    string GridAreaCode,
    ActorNumber EnergySupplierNumber,
    ActorNumber BalanceResponsibleNumber,
    string ProductCode,
    MeasurementUnit QuantityMeasurementUnit,
    Period Period,
    Resolution Resolution,
    IReadOnlyCollection<TimeSeriesPoint> Points,
    SettlementMethod SettlementMethod,
    string? OriginalTransactionIdReference)
{
    public string? OriginalTransactionIdReference { get; set; } = OriginalTransactionIdReference;
}
