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

using AdaskoTheBeAsT.Dapper.NodaTime;
using Dapper;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Configuration.Options;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.DataAccess.DataAccess;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.EDI.DataAccess.Extensions.DependencyInjection;

public static class DatabaseExtensions
{
    public static IServiceCollection AddDapperConnectionToDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<SqlDatabaseConnectionOptions>()
            .Bind(configuration)
            .Validate(o => !string.IsNullOrEmpty(o.DB_CONNECTION_STRING), "DB_CONNECTION_STRING must be set");

        SqlMapper.AddTypeHandler(InstantHandler.Default);

        services
            .AddScoped<IDatabaseConnectionFactory, SqlDatabaseConnectionFactory>()

            // Health checks
            .TryAddSqlServerHealthCheck(configuration);

        return services;
    }
}
