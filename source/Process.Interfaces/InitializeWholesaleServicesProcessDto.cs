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

namespace Energinet.DataHub.EDI.Process.Interfaces;

public record InitializeWholesaleServicesProcessDto(
    string SenderNumber,
    string SenderRoleCode,
    string ReceiverNumber,
    string ReceiverRoleCode,
    string BusinessReason,
    string MessageType,
    string MessageId,
    string CreatedAt,
    string? BusinessType,
    IReadOnlyCollection<InitializeWholesaleServicesSeries> Serie);

public record InitializeWholesaleServicesSeries(
    string Id,
    string StartDateTime,
    string? EndDateTime,
    string? GridAreaCode,
    string? EnergySupplierId,
    string? SettlementSeriesVersion,
    string? Resolution,
    string? ChargeOwner,
    IReadOnlyCollection<InitializeWholesaleServicesChargeType> ChargeTypes);

public record InitializeWholesaleServicesChargeType(string? Id, string? Type);
