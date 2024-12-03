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

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.RSM012;

public record MeteredDateForMeasurementPointMarketActivityRecord(
    string TransactionId,
    string MarketEvaluationPointNumber,
    string MarketEvaluationPointType,
    string? OriginalTransactionIdReferenceId,
    string Product,
    string QuantityMeasureUnit,
    string RegistrationDateTime,
    string Resolution,
    string StartedDateTime,
    string EndedDateTime,
    IReadOnlyList<PointActivityRecord> Points);

public record PointActivityRecord(
    int Position,
    string? Quality,
    int? Quantity);
