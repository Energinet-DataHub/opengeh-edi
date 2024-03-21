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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using NodaTime;

namespace Energinet.DataHub.EDI.MasterData.Domain.MessageDelegations;

public class MessageDelegation
{
    public MessageDelegation(
        int sequenceNumber,
        DocumentType documentType,
        string gridAreaCode,
        Instant startsAt,
        Instant stopsAt,
        ActorNumber delegatedBy,
        ActorRole delegatedByRole,
        ActorNumber delegatedTo,
        ActorRole delegatedToRole)
    {
        SequenceNumber = sequenceNumber;
        DocumentType = documentType;
        GridAreaCode = gridAreaCode;
        StartsAt = startsAt;
        StopsAt = stopsAt;
        DelegatedBy = delegatedBy;
        DelegatedByRole = delegatedByRole;
        DelegatedTo = delegatedTo;
        DelegatedToRole = delegatedToRole;
    }

#pragma warning disable CS8618 // Needed by Entity Framework
    private MessageDelegation()
    {
    }
#pragma warning restore CS8618 // Needed by Entity Framework

    public int SequenceNumber { get; set; }

    public DocumentType DocumentType { get; set; }

    public string GridAreaCode { get; set; }

    public Instant StartsAt { get; set; }

    public Instant StopsAt { get; set; }

    public ActorNumber DelegatedBy { get; set; }

    public ActorRole DelegatedByRole { get; set; }

    public ActorNumber DelegatedTo { get; set; }

    public ActorRole DelegatedToRole { get; set; }
}
