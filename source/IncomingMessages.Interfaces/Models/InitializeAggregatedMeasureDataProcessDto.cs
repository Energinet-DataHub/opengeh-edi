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

namespace Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;

/// <summary>
/// Responsible for carrying the market message data from the incoming message before any data validation.
/// </summary>
public record InitializeAggregatedMeasureDataProcessDto(
    string SenderNumber,
    string SenderRoleCode,
    string BusinessReason,
    string MessageId,
    IReadOnlyCollection<InitializeAggregatedMeasureDataProcessSeries> Series);

public record InitializeAggregatedMeasureDataProcessSeries(
    TransactionId Id,
    string? MeteringPointType,
    string? SettlementMethod,
    string StartDateTime,
    string? EndDateTime,
    string? RequestedGridAreaCode,
    string? EnergySupplierNumber,
    string? BalanceResponsibleNumber,
    string? SettlementVersion,
    IReadOnlyCollection<string> GridAreas,
    RequestedByActor RequestedByActor,
    OriginalActor OriginalActor);
