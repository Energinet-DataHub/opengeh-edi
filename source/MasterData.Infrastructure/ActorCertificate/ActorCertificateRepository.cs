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

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.MasterData.Domain.ActorCertificates;
using Energinet.DataHub.EDI.MasterData.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.EDI.MasterData.Infrastructure.ActorCertificate;

public class ActorCertificateRepository : IActorCertificateRepository
{
    private readonly MasterDataContext _masterDataContext;

    public ActorCertificateRepository(MasterDataContext masterDataContext)
    {
        _masterDataContext = masterDataContext;
    }

    public Task<Domain.ActorCertificates.ActorCertificate?> GetFromThumbprintAsync(CertificateThumbprint thumbprint)
    {
        return _masterDataContext.ActorCertificates
            .Where(ac => ac.Thumbprint == thumbprint)
            .SingleOrDefaultAsync();
    }

    public Task<Domain.ActorCertificates.ActorCertificate?> GetFromActorRoleAsync(
        ActorNumber actorNumber,
        ActorRole actorRole)
    {
        return _masterDataContext.ActorCertificates
            .Where(ac => ac.ActorNumber == actorNumber && ac.ActorRole == actorRole)
            .SingleOrDefaultAsync();
    }

    public void Add(Domain.ActorCertificates.ActorCertificate newActorCertificate)
    {
        _masterDataContext.ActorCertificates.Add(newActorCertificate);
    }

    public async Task DeleteAsync(
        ActorNumber actorNumber,
        CertificateThumbprint certificateThumbprint,
        CancellationToken cancellationToken)
    {
        var actorCertificate = await _masterDataContext.ActorCertificates
            .Where(ac => ac.ActorNumber == actorNumber && ac.Thumbprint == certificateThumbprint)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (actorCertificate != null)
            _masterDataContext.ActorCertificates.Remove(actorCertificate);
    }
}
