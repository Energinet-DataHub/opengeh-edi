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

using System;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Actors;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.Domain.ActorCertificates;
using MediatR;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Energinet.DataHub.EDI.Application.ActorCertificate;

public class ActorCertificateCredentialsAssignedCommandHandler : IRequestHandler<ActorCertificateCredentialsAssignedCommand, Unit>
{
    private readonly ILogger<ActorCertificateCredentialsAssignedCommandHandler> _logger;
    private readonly IActorCertificateRepository _actorCertificateRepository;

    public ActorCertificateCredentialsAssignedCommandHandler(ILogger<ActorCertificateCredentialsAssignedCommandHandler> logger, IActorCertificateRepository actorCertificateRepository)
    {
        _logger = logger;
        _actorCertificateRepository = actorCertificateRepository;
    }

    public async Task<Unit> Handle(ActorCertificateCredentialsAssignedCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var existingCertificate = await _actorCertificateRepository.GetFromActorRoleAsync(request.ActorNumber, request.ActorRole).ConfigureAwait(false);

        if (existingCertificate == null)
        {
            var newCertificate = new Domain.ActorCertificates.ActorCertificate(request.ActorNumber, request.ActorRole, request.Thumbprint, request.ValidFrom, request.SequenceNumber);
            _actorCertificateRepository.Add(newCertificate);
        }
        else if (existingCertificate.SequenceNumber < request.SequenceNumber)
        {
            existingCertificate.Update(request.Thumbprint, request.ValidFrom, request.SequenceNumber);
        }
        else
        {
            _logger.LogInformation("Skip updating actor certificate for actor {ActorNumber} with role {ActorRole}, since the received sequence number {ReceivedSequenceNumber} is lower than the current sequence number {CurrentSequenceNumber}", existingCertificate.ActorNumber.Value, existingCertificate.ActorRole.Name, request.SequenceNumber, existingCertificate.SequenceNumber);
        }

        return Unit.Value;
    }
}
