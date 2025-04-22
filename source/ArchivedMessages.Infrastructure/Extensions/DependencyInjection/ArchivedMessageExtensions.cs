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

using Energinet.DataHub.EDI.ArchivedMessages.Application;
using Energinet.DataHub.EDI.ArchivedMessages.Domain;
using Energinet.DataHub.EDI.ArchivedMessages.Interfaces;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.DataAccess.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.EDI.ArchivedMessages.Infrastructure.Extensions.DependencyInjection;

public static class ArchivedMessageExtensions
{
    public static IServiceCollection AddArchivedMessagesModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddTransient<IArchivedMessageRepository, ArchivedMessageRepository>()
            .AddTransient<IMeteringPointArchivedMessageRepository, MeteringPointArchivedMessageRepository>()
            .AddTransient<IArchivedMessagesClient, ArchivedMessagesClient>()
            .AddDapperConnectionToDatabase(configuration);

        // Dependencies
        services.AddBuildingBlocks(configuration);

        return services;
    }
}
