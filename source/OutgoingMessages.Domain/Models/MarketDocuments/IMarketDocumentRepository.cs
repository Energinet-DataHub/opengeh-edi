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

using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.Bundles;

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.MarketDocuments;

/// <summary>
///  Repository for market documents
/// </summary>
public interface IMarketDocumentRepository
{
    /// <summary>
    /// Get document by bundle id
    /// </summary>
    Task<MarketDocument?> GetAsync(BundleId bundleId);

    /// <summary>
    /// Add document to repository
    /// </summary>
    void Add(MarketDocument marketDocument);

    /// <summary>
    /// Delete market documents if it exists
    /// </summary>
    Task DeleteMarketDocumentsIfExistsAsync(IReadOnlyCollection<BundleId> bundleMessageIds);
}
