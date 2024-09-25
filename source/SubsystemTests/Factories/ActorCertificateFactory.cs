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

using Energinet.DataHub.MarketParticipant.Infrastructure.Model.Contracts;
using Google.Protobuf.WellKnownTypes;

namespace Energinet.DataHub.EDI.SubsystemTests.Factories;

public static class ActorCertificateFactory
{
    public static ActorCertificateCredentialsAssigned CreateActorCertificateAssigned(string actorNumber, string actorRole, string thumbprint) =>
        new()
        {
            ActorNumber = actorNumber,
            ActorRole = GetActorRole(actorRole),
            CertificateThumbprint = thumbprint,
            ValidFrom = DateTime.UtcNow.ToTimestamp(),
            SequenceNumber = 1,
        };

    public static ActorCertificateCredentialsRemoved CreateActorCertificateCredentialsRemoved(string actorNumber, string actorRole, string thumbprint) =>
        new()
        {
            ActorNumber = actorNumber,
            ActorRole = GetActorRole(actorRole),
            CertificateThumbprint = thumbprint,
            SequenceNumber = 1,
            ValidFrom = Timestamp.FromDateTime(DateTime.UtcNow),
        };

    private static EicFunction GetActorRole(string actorRole)
    {
        switch (actorRole)
        {
            case "metereddataresponsible":
                return EicFunction.MeteredDataResponsible;
            default:
                return EicFunction.MeteredDataResponsible;
        }
    }
}
