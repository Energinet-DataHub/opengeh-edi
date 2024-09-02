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

using BuildingBlocks.Application.Extensions.DependencyInjection;
using Energinet.DataHub.Core.App.Common.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.DataAccess.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.Outbox.Domain;
using Energinet.DataHub.EDI.Outbox.Infrastructure;
using Energinet.DataHub.EDI.Outbox.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.EDI.Outbox.Application.Extensions.DependencyInjection;

public static class OutboxExtensions
{
    /// <summary>
    /// Add services required for creating and persisting outbox messages.
    /// </summary>
    public static IServiceCollection AddOutboxModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        services.AddBuildingBlocks(configuration)
            .AddScopedSqlDbContext<OutboxContext>(configuration)
            .AddScoped<BuildingBlocks.Domain.ExecutionContext>();

        // DataRetentionConfiguration
        services.AddTransient<IDataRetention, OutboxRetention>();

        services
            .AddNodaTimeForApplication();

        services.AddTransient<IOutboxRepository, OutboxRepository>();
        services.AddTransient<IOutboxClient, OutboxClient>();

        return services;
    }

    /// <summary>
    /// Add services required for processing and publishing outbox messages.
    /// </summary>
    public static IServiceCollection AddOutboxProcessor(
        this IServiceCollection services)
    {
        services.AddTransient<IOutboxProcessor, OutboxProcessor>();

        return services;
    }
}
