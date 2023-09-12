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

using Domain.Actors;
using Domain.Documents;
using NodaTime;

namespace Domain.ArchivedMessages;

public record ArchivedMessage(
    // This is most likely a guid, but!
    // We have examples of this being a 36 characters string not supporting the guid format in our DB.
    // Hence we keep this as a string
    string Id,
    string? MessageId,
    DocumentType DocumentType,
    ActorNumber? SenderNumber,
    ActorNumber? ReceiverNumber,
    Instant CreatedAt,
    string? BusinessReason,
    Stream Document);
