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
using System.Threading.Tasks;
using Energinet.DataHub.EDI.ApplyDBMigrationsApp.Helpers;
using Energinet.DataHub.EDI.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.Infrastructure.Configuration.Serialization;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Energinet.DataHub.EDI.IntegrationTests.Fixtures
{
    public class ProcessDatabaseFixture : IDisposable, IAsyncLifetime
    {
        private static string _connectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=B2BTransactions;Integrated Security=True;Connection Timeout=60";
        private readonly ProcessContext _processContext;
        private bool _disposed;

        public ProcessDatabaseFixture()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.local.json", optional: true)
                .Build();

            var connectionStringFromConfig = configuration.GetConnectionString("Default");
            if (!string.IsNullOrEmpty(connectionStringFromConfig))
                _connectionString = connectionStringFromConfig;

            var environmentVariableConnectionString = Environment.GetEnvironmentVariable("B2B_MESSAGING_CONNECTION_STRING");
            if (!string.IsNullOrWhiteSpace(environmentVariableConnectionString))
            {
                _connectionString = environmentVariableConnectionString;
            }

            var optionsBuilderProcess = new DbContextOptionsBuilder<ProcessContext>();
            optionsBuilderProcess
                .UseSqlServer(_connectionString, options => options.UseNodaTime());

            _processContext = new ProcessContext(optionsBuilderProcess.Options);
        }

        public static string ConnectionString => _connectionString;

        public Task InitializeAsync()
        {
            CreateSchema();
            CleanupDatabase();
            return Task.CompletedTask;
        }

        public Task DisposeAsync()
        {
            Dispose();
            return Task.CompletedTask;
        }

        public void CleanupDatabase()
        {
            var cleanupStatement =
                $"DELETE FROM [dbo].[MoveInTransactions] " +
                $"DELETE FROM [dbo].[UpdateCustomerMasterDataTransactions] " +
                $"DELETE FROM [dbo].[MessageRegistry] " +
                $"DELETE FROM [dbo].[TransactionRegistry]" +
                $"DELETE FROM [dbo].[OutgoingMessages] " +
                $"DELETE FROM [dbo].[ReasonTranslations] " +
                $"DELETE FROM [dbo].[QueuedInternalCommands] " +
                $"DELETE FROM [dbo].[MarketEvaluationPoints]" +
                $"DELETE FROM [dbo].[Actor]" +
                $"DELETE FROM [dbo].[ReceivedIntegrationEvents]" +
                $"DELETE FROM [dbo].[AggregatedMeasureDataProcesses]" +
                $"DELETE FROM [dbo].[ArchivedMessages]" +
                $"DELETE FROM [dbo].[MarketDocuments]" +
                $"DELETE FROM [dbo].[Bundles]" +
                $"DELETE FROM [dbo].[ActorMessageQueues]" +
                $"DELETE FROM [dbo].[ReceivedInboxEvents]" +
                $"DELETE FROM [dbo].[MessageRegistry]" +
                $"DELETE FROM [dbo].[TransactionRegistry]";

            _processContext.Database.ExecuteSqlRaw(cleanupStatement);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed == true)
            {
                return;
            }

            CleanupDatabase();
            _processContext.Dispose();
            _disposed = true;
        }

        private static void CreateSchema()
        {
            var upgradeResult = DbUpgradeRunner.RunDbUpgrade(ConnectionString);
            if (!upgradeResult.Successful)
                throw new InvalidOperationException("Database upgrade failed", upgradeResult.Error);
        }
    }
}
