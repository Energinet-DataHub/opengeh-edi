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
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DateTime;
using Energinet.DataHub.EDI.MasterData.Infrastructure.DataAccess;
using NodaTime;

namespace Energinet.DataHub.EDI.MasterData.Infrastructure.GridAreas;

public class GridAreaOwnerRetention : IDataRetention
{
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;
    private readonly MasterDataContext _masterDataContext;

    public GridAreaOwnerRetention(
        ISystemDateTimeProvider systemDateTimeProvider,
        MasterDataContext masterDataContext)
    {
        _systemDateTimeProvider = systemDateTimeProvider;
        _masterDataContext = masterDataContext;
    }

    public async Task CleanupAsync(CancellationToken cancellationToken)
    {
       var now = _systemDateTimeProvider.Now();
       var monthAgo = now.Plus(-Duration.FromDays(30));
       _masterDataContext.GridAreaOwners.RemoveRange(
           _masterDataContext.GridAreaOwners
                .Where(x => x.ValidFrom < monthAgo)
                .Where(x => _masterDataContext.GridAreaOwners.Any(y =>
                    y.GridAreaCode == x.GridAreaCode
                    && y.ValidFrom < now
                    && y.SequenceNumber > x.SequenceNumber)));

       await _masterDataContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
