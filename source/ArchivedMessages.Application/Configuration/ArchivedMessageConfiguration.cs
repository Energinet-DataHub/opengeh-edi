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

using Dapper;
using Dapper.NodaTime;
using Energinet.DataHub.EDI.ArchivedMessages.Application.Configuration.Options;
using Energinet.DataHub.EDI.ArchivedMessages.Infrastructure;
using Energinet.DataHub.EDI.ArchivedMessages.Interfaces;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.EDI.ArchivedMessages.Application.Configuration;

public static class ArchivedMessageConfiguration
{
    public static void AddArchivedMessagesModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<SqlDatabaseConnectionOptions>()
            .Bind(configuration)
            .Validate(o => !string.IsNullOrEmpty(o.DB_CONNECTION_STRING), "DB_CONNECTION_STRING must be set");

        services.AddSingleton<IDatabaseConnectionFactory, SqlDatabaseConnectionFactory>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<SqlDatabaseConnectionOptions>>();
            return new SqlDatabaseConnectionFactory(options.Value.DB_CONNECTION_STRING);
        });
        services.AddTransient<IArchivedMessageRepository, ArchivedMessageRepository>();
        services.AddTransient<IArchivedMessagesClient, ArchivedMessagesClient>();
        SqlMapper.AddTypeHandler(InstantHandler.Default);
    }
}
