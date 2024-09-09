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

using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Configuration;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Configuration.Options;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.EDI.DataAccess.Extensions.DependencyInjection;

public static class EntityFrameworkExtensions
{
    public static IServiceCollection AddScopedSqlDbContext<TDbContext>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TDbContext : Microsoft.EntityFrameworkCore.DbContext, IEdiDbContext
    {
        services
            .AddOptions<SqlDatabaseConnectionOptions>()
            .Bind(configuration)
            .Validate(o => !string.IsNullOrEmpty(o.DB_CONNECTION_STRING), "DB_CONNECTION_STRING must be set");

        services.AddScoped<SqlConnectionSource>()
            .AddDbContext<TDbContext>((sp, o) =>
            {
                var source = sp.GetRequiredService<SqlConnectionSource>();
                o.UseSqlServer(source.Connection, y => y.UseNodaTime());
            });

        // Add as IEdiDbContext to enable UnitOfWork to get all registered DbContexts
        services.AddTransient<IEdiDbContext, TDbContext>(sp => sp.GetRequiredService<TDbContext>());

        services.TryAddSqlServerHealthCheck(configuration);

        return services;
    }
}
