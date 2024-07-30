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

using NodaTime;

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.Bundles;

/// <summary>
/// The repository for bundles
/// </summary>
public interface IBundleRepository
{
    /// <summary>
    /// Add a new Bundle
    /// </summary>
    void Add(Bundle bundle);

    /// <summary>
    ///  Get dequeued bundles older than a specific time.
    /// </summary>
    /// <param name="olderThan"></param>
    /// <param name="take"></param>
    Task<IReadOnlyCollection<Bundle?>> GetDequeuedBundlesOlderThanAsync(Instant olderThan, int take);

    /// <summary>
    ///  Delete a bundle.
    /// </summary>
    /// <param name="bundle"></param>
    void Delete(Bundle bundle);
}
