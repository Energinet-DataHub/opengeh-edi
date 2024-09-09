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

using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Process.Domain.Transactions.WholesaleServices;

namespace Energinet.DataHub.EDI.Process.Domain.Wholesale;

/// <summary>
/// Interface for wholesale inbox
/// </summary>
public interface IWholesaleInbox
{
    /// <summary>
    /// Send <paramref name="aggregatedMeasureDataProcess"/> to wholesale
    /// </summary>
    Task SendProcessAsync(AggregatedMeasureDataProcess aggregatedMeasureDataProcess, CancellationToken cancellationToken);

    /// <summary>
    /// Send <paramref name="wholesaleServicesProcess"/> to wholesale
    /// </summary>
    Task SendProcessAsync(WholesaleServicesProcess wholesaleServicesProcess, CancellationToken cancellationToken);
}
