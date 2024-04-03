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

namespace Energinet.DataHub.EDI.IncomingMessages.Domain.Messages;

public record RequestAggregatedMeasureDataMessage(
    string SenderNumber,
    string SenderRoleCode,
    string ReceiverNumber,
    string ReceiverRoleCode,
    string BusinessReason,
    string MessageType,
    string MessageId,
    string CreatedAt,
    string? BusinessType,
    IReadOnlyCollection<IIncomingMessageSerie> Serie) : IIncomingMessage;

public record RequestAggregatedMeasureDataSerie(
    string TransactionId,
    string? MarketEvaluationPointType,
    string? MarketEvaluationSettlementMethod,
    string StartDateTime,
    string? EndDateTime,
    string? MeteringGridAreaDomainId,
    string? EnergySupplierMarketParticipantId,
    string? BalanceResponsiblePartyMarketParticipantId,
    string? SettlementVersion) : IIncomingMessageSerie;
