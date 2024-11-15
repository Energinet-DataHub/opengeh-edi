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

namespace Energinet.DataHub.EDI.MasterData.Domain.ActorCertificates;

public class ActorCertificate
{
    private readonly Guid _id;

    public ActorCertificate(ActorNumber actorNumber, ActorRole actorRole, CertificateThumbprint thumbprint, Instant validFrom, int sequenceNumber)
    {
        _id = Guid.NewGuid();
        ActorNumber = actorNumber;
        ActorRole = actorRole;
        Thumbprint = thumbprint;
        ValidFrom = validFrom;
        SequenceNumber = sequenceNumber;
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    // ReSharper disable once UnusedMember.Local
    private ActorCertificate() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public ActorNumber ActorNumber { get; }

    public ActorRole ActorRole { get; }

    public CertificateThumbprint Thumbprint { get; private set; }

    public Instant ValidFrom { get; private set; }

    /// <summary>
    /// Sequence number is used to determine which certificate is the newest.
    /// </summary>
    public int SequenceNumber { get; private set; }

    public void Update(CertificateThumbprint thumbprint, Instant validFrom, int sequenceNumber)
    {
        if (sequenceNumber < SequenceNumber) return;
        Thumbprint = thumbprint;
        ValidFrom = validFrom;
        SequenceNumber = sequenceNumber;
    }
}
