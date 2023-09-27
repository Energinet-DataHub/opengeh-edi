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

using Energinet.DataHub.EDI.Domain.Actors;
using Energinet.DataHub.EDI.Domain.OutgoingMessages;
using NodaTime;

namespace Energinet.DataHub.EDI.Domain.Documents;

public record RequestAggregatedMeasureDataMarketMessage(
    ActorNumber SenderNumber,
    MarketRole SenderRole,
    ActorNumber ReceiverNumber,
    MarketRole ReceiverRole,
    string BusinessReason,
    string? AuthenticatedUser,
    string? AuthenticatedUserRole,
    string MessageType,
    string MessageId,
    IReadOnlyCollection<Serie> Series) : MarketMessage(SenderNumber, SenderRole, ReceiverNumber, ReceiverRole, BusinessReason, AuthenticatedUser, AuthenticatedUserRole, MessageType, MessageId, Series);

public record MarketMessage(
    ActorNumber SenderNumber,
    MarketRole SenderRole,
    ActorNumber ReceiverNumber,
    MarketRole ReceiverRole,
    string BusinessReason,
    string? AuthenticatedUser,
    string? AuthenticatedUserRole,
    string MessageType,
    string MessageId,
    IReadOnlyCollection<Serie> MarketTransactions);

public record Serie(
    string Id,
    string? MarketEvaluationPointType,
    string? MarketEvaluationSettlementMethod,
    Instant StartDateAndOrTimeDateTime,
    Instant? EndDateAndOrTimeDateTime,
    string? MeteringGridAreaDomainId,
    string? EnergySupplierMarketParticipantId,
    string? BalanceResponsiblePartyMarketParticipantId);
