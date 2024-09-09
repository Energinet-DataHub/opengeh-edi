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
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;
using Resolution = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Resolution;
using SettlementVersion = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.SettlementVersion;

namespace Energinet.DataHub.EDI.IntegrationTests.DocumentAsserters;

public record NotifyWholesaleServicesDocumentAssertionInput(
    string Timestamp,
    BusinessReasonWithSettlementVersion BusinessReasonWithSettlementVersion,
    string ReceiverId,
    ActorRole ReceiverRole,
    string SenderId,
    ActorRole SenderRole,
    string? ChargeTypeOwner,
    string? ChargeCode,
    ChargeType? ChargeType,
    Currency Currency,
    string EnergySupplierNumber,
    SettlementMethod? SettlementMethod,
    MeteringPointType? MeteringPointType,
    string GridArea,
    TransactionId? OriginalTransactionIdReference,
    MeasurementUnit? PriceMeasurementUnit,
    string ProductCode,
    MeasurementUnit QuantityMeasurementUnit,
    long CalculationVersion,
    Resolution Resolution,
    Period Period,
    IReadOnlyCollection<WholesaleServicesRequestSeries.Types.Point> Points);

public record BusinessReasonWithSettlementVersion(
    BusinessReason BusinessReason,
    SettlementVersion? SettlementVersion);
