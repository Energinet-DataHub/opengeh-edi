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
using Energinet.DataHub.EDI.Common.Actors;
using Energinet.DataHub.EDI.Domain.GridAreas;
using Energinet.DataHub.EDI.Infrastructure.Configuration.DataAccess;
using NodaTime;

namespace Energinet.DataHub.EDI.IntegrationTests.Factories;

public class GridAreaBuilder
{
    private static readonly Instant _fromDateTimeUtc = Instant.FromDateTimeUtc(DateTime.UtcNow);
    private static string _gridArea = "543";
    private static ActorNumber _actorNumber = ActorNumber.Create("5148796574821");

#pragma warning disable CA1822
    public void Store(B2BContext b2BContext)
#pragma warning restore CA1822
    {
        if (b2BContext == null) throw new ArgumentNullException(nameof(b2BContext));
        var gridArea = Build();
        b2BContext.GridAreas.Add(gridArea);
        b2BContext.SaveChanges();
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

    private static GridArea Build()
    {
        return new GridArea(_gridArea, _fromDateTimeUtc, _actorNumber);
    }
}
