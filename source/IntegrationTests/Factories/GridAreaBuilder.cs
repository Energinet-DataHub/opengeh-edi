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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.MasterData.Interfaces.Models;
using Microsoft.EntityFrameworkCore.SqlServer.NodaTime.Extensions;
using NodaTime;

namespace Energinet.DataHub.EDI.IntegrationTests.Factories;

public class GridAreaBuilder
{
    private static readonly Instant _fromDateTimeUtc = Instant.FromDateTimeUtc(DateTime.UtcNow).PlusMinutes(-1);
    private static string _gridArea = "543";
    private static ActorNumber _actorNumber = ActorNumber.Create("5148796574821");

#pragma warning disable CA1822
    public Task StoreAsync(IMasterDataClient masterDataContext)
#pragma warning restore CA1822
    {
        ArgumentNullException.ThrowIfNull(masterDataContext);
        var gridArea = Build();
        return masterDataContext
            .UpdateGridAreaOwnershipAsync(gridArea, CancellationToken.None);
    }

    public GridAreaBuilder WithGridAreaCode(string gridAreaCode)
    {
        _gridArea = gridAreaCode;
        return this;
    }

    public GridAreaBuilder WithActorNumber(ActorNumber actorNumber)
    {
        _actorNumber = actorNumber;
        return this;
    }

    private static GridAreaOwnershipAssignedDto Build()
    {
        return new GridAreaOwnershipAssignedDto(_gridArea, _fromDateTimeUtc, _actorNumber, 1);
    }
}
