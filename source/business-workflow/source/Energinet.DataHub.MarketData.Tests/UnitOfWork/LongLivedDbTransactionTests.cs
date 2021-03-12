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
using System.Data.Common;
using System.Threading.Tasks;
using Dapper;
using Energinet.DataHub.MarketData.Infrastructure.DataPersistence;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Energinet.DataHub.MarketData.Tests.UnitOfWork
{
    [Trait("Category", "Unit")]
    public sealed class LongLivedDbTransactionTests : IDisposable
    {
        private DbConnection? _keepConnectionOpen;

        [Fact]
        public async Task Command_should_write_data_to_database()
        {
            var connectionString = CreateUniqueConnectionString();
            await InitializeDatabase(connectionString);

            using var sut = new LongLivedDbTransaction(() => new SqliteConnection(connectionString));
            await sut.ExecuteAsync(new InsertRandomText());
            await sut.CompleteAsync();

            var recordCount = await _keepConnectionOpen.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM TextEntries");

            recordCount.Should().Be(1);
        }

        [Fact]
        public async Task Command_should_not_be_written_if_aborted()
        {
            var connectionString = CreateUniqueConnectionString();
            await InitializeDatabase(connectionString);

            using var sut = new LongLivedDbTransaction(() => new SqliteConnection(connectionString));
            await sut.ExecuteAsync(new InsertRandomText());
            await sut.AbortAsync();

            var recordCount = await _keepConnectionOpen.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM TextEntries");
            recordCount.Should().Be(0);
        }

        [Fact]
        public async Task Completing_more_than_once_should_only_write_once()
        {
            var connectionString = CreateUniqueConnectionString();
            await InitializeDatabase(connectionString);

            using var sut = new LongLivedDbTransaction(() => new SqliteConnection(connectionString));
            await sut.ExecuteAsync(new InsertRandomText());
            await sut.CompleteAsync();
            await sut.CompleteAsync();

            var recordCount = await _keepConnectionOpen.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM TextEntries");
            recordCount.Should().Be(1);
        }

        [Fact]
        public async Task Query_should_return_data_from_previous_command()
        {
            var connectionString = CreateUniqueConnectionString();
            await InitializeDatabase(connectionString);

            using var sut = new LongLivedDbTransaction(() => new SqliteConnection(connectionString));
            await sut.ExecuteAsync(new InsertRandomText());
            var recordCount = await sut.QueryAsync(new CountTextEntries());

            recordCount.Should().Be(1);
        }

        [Fact]
        public async Task Abort_then_complete_should_not_write_data()
        {
            var connectionString = CreateUniqueConnectionString();
            await InitializeDatabase(connectionString);

            using var sut = new LongLivedDbTransaction(() => new SqliteConnection(connectionString));
            await sut.ExecuteAsync(new InsertRandomText());
            await sut.AbortAsync();
            await sut.CompleteAsync();

            var recordCount = await _keepConnectionOpen.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM TextEntries");
            recordCount.Should().Be(0);
        }

        public void Dispose()
        {
            _keepConnectionOpen?.Dispose();
        }

        private async ValueTask InitializeDatabase(string connectionString)
        {
            _keepConnectionOpen = new SqliteConnection(connectionString);
            await _keepConnectionOpen.OpenAsync();

            await _keepConnectionOpen.ExecuteAsync("CREATE TABLE IF NOT EXISTS TextEntries (TextData TEXT)");
        }

        private string CreateUniqueConnectionString() => $"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared";
    }
}
