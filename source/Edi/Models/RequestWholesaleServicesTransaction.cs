﻿// Copyright 2020 Energinet DataHub A/S
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
using Energinet.DataHub.EDI.Process.Domain.Transactions;
using Energinet.DataHub.EDI.Process.Interfaces;

namespace Energinet.DataHub.Wholesale.Edi.Models;

public record RequestWholesaleServicesTransaction(
    ProcessId ProcessId,
    RequestedByActor RequestedByActor,
    OriginalActor OriginalActor,
    TransactionId BusinessTransactionId,
    MessageId InitiatedByMessageId,
    BusinessReason BusinessReason,
    string StartOfPeriod,
    string? EndOfPeriod,
    string? RequestedGridArea,
    string? EnergySupplierId,
    SettlementVersion? SettlementVersion,
    string? Resolution,
    string? ChargeOwner,
    IReadOnlyCollection<Energinet.DataHub.EDI.Process.Domain.Transactions.WholesaleServices.ChargeType> ChargeTypes,
    IReadOnlyCollection<string> GridAreas);
