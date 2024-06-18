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

namespace Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;

/// <summary>
/// Responsible for carrying the market message data from the incoming message before any data validation.
/// </summary>
public record RequestAggregatedMeasureDataDto(
    string SenderNumber,
    string SenderRoleCode,
    string ReceiverNumber,
    string ReceiverRoleCode,
    string BusinessReason,
    string MessageType,
    string MessageId,
    string CreatedAt,
    string? BusinessType,
    IReadOnlyCollection<RequestAggregatedMeasureDataSeries> Serie);

public record RequestAggregatedMeasureDataSeries(
    string Id,
    string? MarketEvaluationPointType,
    string? MarketEvaluationSettlementMethod,
    string StartDateAndOrTimeDateTime,
    string? EndDateAndOrTimeDateTime,
    string? MeteringGridAreaDomainId,
    string? EnergySupplierMarketParticipantId,
    string? BalanceResponsiblePartyMarketParticipantId,
    string? SettlementVersion);
