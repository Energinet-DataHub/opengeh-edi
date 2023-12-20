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

using BuildingBlocks.Application.Configuration;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure;
using Energinet.DataHub.EDI.MasterData.Domain.ActorCertificates;
using Energinet.DataHub.EDI.MasterData.Domain.Actors;
using Energinet.DataHub.EDI.MasterData.Domain.GridAreaOwners;
using Energinet.DataHub.EDI.MasterData.Infrastructure.ActorCertificate;
using Energinet.DataHub.EDI.MasterData.Infrastructure.Actors;
using Energinet.DataHub.EDI.MasterData.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.MasterData.Infrastructure.GridAreas;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.EDI.MasterData.Application.Configuration;

public static class MasterDataConfiguration
{
    public static void AddMasterDataModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScopedSqlDbContext<MasterDataContext>(configuration);

        // Grid area
        services.AddTransient<IGridAreaRepository, GridAreaRepository>();
        services.AddTransient<IDataRetention, GridAreaOwnerRetention>();

        // Actors
        services.AddTransient<IActorRepository, ActorRepository>();
        services.AddTransient<IActorCertificateRepository, ActorCertificateRepository>();

        services.AddTransient<IMasterDataClient, MasterDataClient>();
    }
}
