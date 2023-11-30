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

using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.Application.ActorCertificate;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Actors;
using Energinet.DataHub.EDI.Infrastructure.Configuration.DataAccess;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Energinet.DataHub.EDI.Infrastructure.ActorCertificate;

public class ActorCertificateRepository : IActorCertificateRepository
{
    private readonly B2BContext _dbContext;

    public ActorCertificateRepository(B2BContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task CreateOrUpdateAsync(ActorNumber actorNumber, MarketRole role, string thumbprint, Instant validFrom, int sequenceNumber)
    {
        var existingActorCertificate = await _dbContext.ActorCertificates
            .Where(ac => ac.ActorNumber == actorNumber && ac.ActorRole == role)
            .SingleOrDefaultAsync().ConfigureAwait(false);

        if (existingActorCertificate == null)
        {
            var newActorCertificate = new Domain.ActorCertificate(actorNumber, role, thumbprint, validFrom, sequenceNumber);
            _dbContext.ActorCertificates.Add(newActorCertificate);
        }
        else if (existingActorCertificate.SequenceNumber < sequenceNumber)
        {
            existingActorCertificate.Update(thumbprint, validFrom, sequenceNumber);
        }
    }

    public Task<Domain.ActorCertificate?> GetFromThumbprintAsync(string thumbprint)
    {
        return _dbContext.ActorCertificates
            .Where(ac => ac.Thumbprint == thumbprint)
            .SingleOrDefaultAsync();
    }
}
