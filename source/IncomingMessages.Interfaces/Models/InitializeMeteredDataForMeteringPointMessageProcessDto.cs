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

namespace Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;

public record InitializeMeteredDataForMeteringPointMessageProcessDto(
    string MessageId,
    string MessageType,
    string CreatedAt,
    string BusinessReason,
    string? BusinessType,
    IReadOnlyCollection<InitializeMeteredDataForMeteringPointMessageSeries> Series);

public record InitializeMeteredDataForMeteringPointMessageSeries(
    string TransactionId,
    string? Resolution,
    string StartDateTime,
    string? EndDateTime,
    string? ProductNumber,
    string? ProductUnitType,
    string? MeteringPointType,
    string? MeteringPointLocationId,
    string? RegisteredAt,
    // DelegatedGridAreaCodes is a list of grid area codes that the requester is allowed to submit measurements for
    IReadOnlyCollection<string>? DelegatedGridAreaCodes,
    RequestedByActor RequestedByActor,
    IReadOnlyCollection<InitializeEnergyObservation> EnergyObservations);

public record InitializeEnergyObservation(
    string? Position,
    string? EnergyQuantity,
    string? QuantityQuality);
