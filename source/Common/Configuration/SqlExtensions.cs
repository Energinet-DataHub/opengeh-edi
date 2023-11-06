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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.EDI.Common.Configuration;

public static class SqlExtensions
{
    public static void AddScopedSqlDbContext<TDbContext>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder<TDbContext>>? optionsAction = null)
        where TDbContext : DbContext
    {
        services.AddScoped<DbContextOptions<TDbContext>>(serviceProvider =>
        {
            var builder = new DbContextOptionsBuilder<TDbContext>();
            var source = serviceProvider.GetRequiredService<SqlConnectionSource>();
            builder.UseSqlServer(source.Connection, y => y.UseNodaTime());
            optionsAction?.Invoke(builder);

            return builder
                .Options;
        });

        services.AddScoped<TDbContext>();
    }
}
