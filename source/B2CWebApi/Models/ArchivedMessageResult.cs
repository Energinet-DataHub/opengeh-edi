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

namespace Energinet.DataHub.EDI.B2CWebApi.Models;

[Serializable]
public record ArchivedMessageResultV2(
    long RecordId,
    string Id,
    string? MessageId,
    string DocumentType,
    string SenderNumber,
    string ReceiverNumber,
    DateTimeOffset CreatedAt,
    string? BusinessReason);

[Serializable]
public record ArchivedMessageResultV3(
    long RecordId,
    string Id,
    string? MessageId,
    DocumentType DocumentType,
    string SenderNumber,
    ActorRole? SenderRole,
    string ReceiverNumber,
    ActorRole? ReceiverRole,
    DateTimeOffset CreatedAt,
    string? BusinessReason);
