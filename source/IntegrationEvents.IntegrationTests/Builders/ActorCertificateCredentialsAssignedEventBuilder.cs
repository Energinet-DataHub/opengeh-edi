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
using Energinet.DataHub.MarketParticipant.Infrastructure.Model.Contracts;
using NodaTime;
using NodaTime.Serialization.Protobuf;

namespace Energinet.DataHub.EDI.IntegrationTests.Factories;

internal sealed class ActorCertificateCredentialsAssignedEventBuilder
{
    internal ActorNumber ActorNumber { get; private set; } = ActorNumber.Create("1234567890123");

    internal EicFunction ActorRole { get; private set; } = EicFunction.EnergySupplier;

    internal string CertificateThumbprint { get; private set; } = "12345";

    internal Instant ValidFrom { get; private set; } = Instant.FromUtc(2023, 12, 1, 0, 0);

    internal int SequenceNumber { get; private set; } = 1;

    internal ActorCertificateCredentialsAssigned Build()
    {
        return new ActorCertificateCredentialsAssigned()
        {
            ActorNumber = ActorNumber.Value,
            ActorRole = ActorRole,
            CertificateThumbprint = CertificateThumbprint,
            ValidFrom = ValidFrom.ToTimestamp(),
            SequenceNumber = SequenceNumber,
        };
    }

    internal ActorCertificateCredentialsAssignedEventBuilder SetActorNumber(string actorNumber)
    {
        ActorNumber = ActorNumber.Create(actorNumber);
        return this;
    }

    internal ActorCertificateCredentialsAssignedEventBuilder SetActorRole(EicFunction actorRole)
    {
        ActorRole = actorRole;
        return this;
    }

    internal ActorCertificateCredentialsAssignedEventBuilder SetCertificateThumbprint(string thumbprint)
    {
        CertificateThumbprint = thumbprint;
        return this;
    }

    internal ActorCertificateCredentialsAssignedEventBuilder SetValidFrom(Instant validFrom)
    {
        ValidFrom = validFrom;
        return this;
    }

    internal ActorCertificateCredentialsAssignedEventBuilder SetSequenceNumber(int sequenceNumber)
    {
        SequenceNumber = sequenceNumber;
        return this;
    }
}
