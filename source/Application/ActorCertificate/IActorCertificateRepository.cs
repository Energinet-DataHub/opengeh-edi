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

using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Actors;
using NodaTime;

namespace Energinet.DataHub.EDI.Application.ActorCertificate;

/// <summary>
/// Repository for looking up actor certificates
/// </summary>
public interface IActorCertificateRepository
{
    /// <summary>
    /// Create or update actor certificate from ActorCertificateCredentialsAssigned integration event
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    Task CreateOrUpdateAsync(ActorNumber actorNumber, MarketRole role, string thumbprint, Instant validFrom, int sequenceNumber);

    /// <summary>
    /// Get actor certificate from thumbprint
    /// </summary>
    /// <param name="thumbprint"></param>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
    Task<Domain.ActorCertificate?> GetFromThumbprintAsync(string thumbprint);
}
