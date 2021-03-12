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
using System.Data.SqlClient;
using System.Linq;
using Energinet.DataHub.MarketData.Infrastructure.DataPersistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.MarketData.EntryPoint
{
    public static class UnitOfWorkServiceRegistration
    {
        public const string UnitOfWorkType = "UnitOfWorkType";
        public const string ShortLivedTransaction = "ShortLivedTransaction";
        public const string LongLivedTransaction = "LongLivedTransaction";
        public const string DefaultUnitOfWorkTransactionType = ShortLivedTransaction;

        private delegate void ConfigureUnitOfWork(IServiceCollection services, string connectionStringConfigurationName);

        /// <summary>
        /// Registers a unit of work with scoped lifetime.
        /// <remarks>
        /// When performing the registration the <see cref="IConfiguration"/> is checked for a key with the value from <see cref="UnitOfWorkType"/>
        /// If no configuration is found, a registration is created for <see cref="DefaultUnitOfWorkTransactionType"/>.
        /// The default <see cref="IUnitOfWork"/> is <see cref="ShortLivedDbTransaction"/>.
        /// </remarks>
        /// </summary>
        /// <param name="services">ServiceCollection where the registration should be placed</param>
        /// <param name="connectionStringConfigurationName">ConnectionString configuration name</param>
        /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="connectionStringConfigurationName"/> is null</exception>
        public static void AddUnitOfWork(this IServiceCollection services, string connectionStringConfigurationName)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (connectionStringConfigurationName == null) throw new ArgumentNullException(nameof(connectionStringConfigurationName));

            var transactionType = GetUnitOfWorkType(services) ?? DefaultUnitOfWorkTransactionType;

            ConfigureUnitOfWork configuration = transactionType switch
            {
                ShortLivedTransaction => ConfigureShortLivedTransaction,
                LongLivedTransaction => ConfigureLongLivedTransaction,
                _ => throw new InvalidOperationException($"Unsupported transaction type {transactionType}")
            };

            configuration.Invoke(services, connectionStringConfigurationName);
        }

        private static void ConfigureShortLivedTransaction(
            IServiceCollection services,
            string connectionStringConfigurationName)
        {
            services.AddScoped(sp =>
            {
                var configuration = sp.GetRequiredService<IConfiguration>();
                var connectionString =
                    configuration.GetConnectionStringOrSetting(connectionStringConfigurationName);
                return new ShortLivedDbTransaction(() => new SqlConnection(connectionString));
            });

            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ShortLivedDbTransaction>());
            services.AddScoped<IDataWriter>(sp => sp.GetRequiredService<ShortLivedDbTransaction>());
        }

        private static void ConfigureLongLivedTransaction(
            IServiceCollection services,
            string connectionStringConfigurationName)
        {
            services.AddScoped(sp =>
            {
                var configuration = sp.GetRequiredService<IConfiguration>();
                var connectionString =
                    configuration.GetConnectionStringOrSetting(connectionStringConfigurationName);
                return new LongLivedDbTransaction(() => new SqlConnection(connectionString));
            });

            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<LongLivedDbTransaction>());
            services.AddScoped<IDataWriter>(sp => sp.GetRequiredService<LongLivedDbTransaction>());
        }

        private static string? GetUnitOfWorkType(IServiceCollection services)
        {
            var description = services
                .FirstOrDefault(registration => registration.ServiceType == typeof(IConfiguration));

            if (!(description?.ImplementationInstance is IConfiguration configuration)) return null;

            return configuration[UnitOfWorkType];
        }
    }
}
