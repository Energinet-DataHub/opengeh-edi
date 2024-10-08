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

using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.DataAccess.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.EDI.Outbox.Infrastructure;

public static class OutboxExtensions
{
    public static IServiceCollection AddOutboxRetention(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddTransient<IDataRetention, OutboxRetention>();

        return serviceCollection;
    }

    public static IServiceCollection AddOutboxContext(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        serviceCollection.AddScopedSqlDbContext<OutboxContext>(configuration);

        return serviceCollection;
    }
}
