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

using Energinet.DataHub.EDI.ArchivedMessages.Interfaces;
using NodaTime;

namespace Energinet.DataHub.EDI.ArchivedMessages.IntegrationTests.Models;

/// <summary>
/// Using to represent an archived message from the database.
/// Which enables us to use a typed model in test.
/// </summary>
public record ArchivedMessageFromDb(
    Guid Id,
    string? MessageId,
    string DocumentType,
    string SenderNumber,
    string SenderRoleCode,
    string ReceiverNumber,
    string ReceiverRoleCode,
    Instant CreatedAt,
    string? BusinessReason,
    string FileStorageReference,
    string? RelatedToMessageId,
    string? EventIds)
    : MessageInfo(
    Id,
    MessageId,
    DocumentType,
    SenderNumber,
    ReceiverNumber,
    CreatedAt,
    BusinessReason);
