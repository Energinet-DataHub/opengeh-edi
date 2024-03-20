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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.MasterData.Domain.ActorCertificates;
using Energinet.DataHub.EDI.MasterData.Domain.Actors;
using Energinet.DataHub.EDI.MasterData.Domain.GridAreaOwners;
using Energinet.DataHub.EDI.MasterData.Domain.MessageDelegations;
using Energinet.DataHub.EDI.MasterData.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.MasterData.Interfaces.Models;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Energinet.DataHub.EDI.MasterData.Application;

internal sealed class MasterDataClient : IMasterDataClient
{
    private readonly IActorRepository _actorRepository;
    private readonly IGridAreaRepository _gridAreaRepository;
    private readonly IActorCertificateRepository _actorCertificateRepository;
    private readonly MasterDataContext _masterDataContext;
    private readonly ILogger<IMasterDataClient> _logger;
    private readonly IMessageDelegationRepository _messageDelegationRepository;

    public MasterDataClient(
        IActorRepository actorRepository,
        IGridAreaRepository gridAreaRepository,
        IActorCertificateRepository actorCertificateRepository,
        MasterDataContext masterDataContext,
        ILogger<IMasterDataClient> logger,
        IMessageDelegationRepository messageDelegationRepository)
    {
        _actorRepository = actorRepository;
        _gridAreaRepository = gridAreaRepository;
        _actorCertificateRepository = actorCertificateRepository;
        _masterDataContext = masterDataContext;
        _logger = logger;
        _messageDelegationRepository = messageDelegationRepository;
    }

    public async Task CreateActorIfNotExistAsync(CreateActorDto createActorDto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(createActorDto);

        await _actorRepository.CreateIfNotExistAsync(
                createActorDto.ActorNumber,
                createActorDto.ExternalId,
                cancellationToken)
            .ConfigureAwait(false);

        await _masterDataContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<ActorNumber?> GetActorNumberByExternalIdAsync(
        string externalId,
        CancellationToken cancellationToken)
    {
        return _actorRepository.GetActorNumberByExternalIdAsync(externalId, cancellationToken);
    }

    public async Task UpdateGridAreaOwnershipAsync(
        GridAreaOwnershipAssignedDto gridAreaOwnershipAssignedDto,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(gridAreaOwnershipAssignedDto);

        await _gridAreaRepository.UpdateOwnershipAsync(
                gridAreaOwnershipAssignedDto.GridAreaCode,
                gridAreaOwnershipAssignedDto.ValidFrom,
                gridAreaOwnershipAssignedDto.GridAreaOwner,
                gridAreaOwnershipAssignedDto.SequenceNumber,
                cancellationToken)
            .ConfigureAwait(false);

        await _masterDataContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<ActorNumber> GetGridOwnerForGridAreaCodeAsync(string gridAreaCode, CancellationToken cancellationToken)
    {
        return _gridAreaRepository.GetGridOwnerForAsync(gridAreaCode, cancellationToken);
    }

    public async Task CreateOrUpdateActorCertificateAsync(
        ActorCertificateCredentialsAssignedDto request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var existingCertificate = await _actorCertificateRepository
            .GetFromActorRoleAsync(request.ActorNumber, request.ActorRole)
            .ConfigureAwait(false);

        if (existingCertificate == null)
        {
            CreateNewActorCertificate(request);
        }
        else
        {
            existingCertificate.Update(
                new CertificateThumbprint(request.ThumbprintDto.Thumbprint),
                request.ValidFrom,
                request.SequenceNumber);
        }

        await _masterDataContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<ActorNumberAndRoleDto?> GetActorNumberAndRoleFromThumbprintAsync(
        CertificateThumbprintDto thumbprint)
    {
        var actorCertificate =
            await _actorCertificateRepository
                .GetFromThumbprintAsync(new CertificateThumbprint(thumbprint.Thumbprint))
                .ConfigureAwait(false);

        return actorCertificate is not null
            ? new ActorNumberAndRoleDto(actorCertificate.ActorNumber, actorCertificate.ActorRole)
            : null;
    }

    public async Task DeleteActorCertificateAsync(
        ActorCertificateCredentialsRemovedDto actorCertificateCredentialsRemovedDto,
        CancellationToken cancellationToken)
    {
        await _actorCertificateRepository
            .DeleteAsync(
                actorCertificateCredentialsRemovedDto.ActorNumber,
                new CertificateThumbprint(actorCertificateCredentialsRemovedDto.ThumbprintDto.Thumbprint),
                cancellationToken)
            .ConfigureAwait(false);

        await _masterDataContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task CreateMessageDelegationConfiguredAsync(
        MessageDelegationDto messageDelegationDto,
        CancellationToken cancellationToken)
    {
        await _messageDelegationRepository.CreateAsync(
            new MessageDelegation(),
            cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<MessageDelegationDto?> GetMessageDelegationAsync(
        ActorNumber delegatedByActorNumber,
        ActorRole delegatedByActorRole,
        string gridAreaCode,
        DocumentType documentType,
        Instant now)
    {
        return Task.FromResult<MessageDelegationDto?>(null);
        // var messageDelegation = await _messageDelegationRepository.GetAsync(
        //     delegatedByActorNumber,
        //     delegatedByActorRole,
        //     gridAreaCode,
        //     documentType,
        //     now).ConfigureAwait(false);

        // return messageDelegation is not null ? new MessageDelegationDto(messageDelegation) : null;
    }

    private void CreateNewActorCertificate(ActorCertificateCredentialsAssignedDto request)
    {
        var newCertificate = new ActorCertificate(
            request.ActorNumber,
            request.ActorRole,
            new CertificateThumbprint(request.ThumbprintDto.Thumbprint),
            request.ValidFrom,
            request.SequenceNumber);

        _actorCertificateRepository.Add(newCertificate);
    }
}
