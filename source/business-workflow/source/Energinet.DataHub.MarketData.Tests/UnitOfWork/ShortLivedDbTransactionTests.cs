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
using Microsoft.Data.Sqlite;
using Xunit;

namespace Energinet.DataHub.MarketData.Tests.UnitOfWork
{
    [Trait("Category", "Unit")]
    public sealed class ShortLivedDbTransactionTests : IDisposable
    {
        private const int ZeroRecordsShouldBeFound = 0;
        private const int OneRecordShouldBeFound = 1;
        private DbConnection? _keepConnectionOpen;

        [Fact]
        public async Task Command_should_be_written_to_database_if_completed()
        {
            // arrange
            var connectionString = $"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared";
            await InitializeDatabase(connectionString);
            var database = new SqliteConnection(connectionString);

            // act
            using var sut = new ShortLivedDbTransaction(() => database);
            await sut.ExecuteAsync(new InsertRandomText());
            await sut.CompleteAsync();

            // assert
            var recordsFound = await _keepConnectionOpen.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM TextEntries");
            Assert.Equal(OneRecordShouldBeFound, recordsFound);
        }

        [Fact]
        public async Task Command_should_not_be_written_if_aborted()
        {
            // arrange
            var connectionString = $"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared";
            await InitializeDatabase(connectionString);
            var database = new SqliteConnection(connectionString);

            // act
            using var sut = new ShortLivedDbTransaction(() => database);
            await sut.ExecuteAsync(new InsertRandomText());
            await sut.AbortAsync();

            // assert
            var recordsFound = await _keepConnectionOpen.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM TextEntries");

            Assert.Equal(ZeroRecordsShouldBeFound, recordsFound);
        }

        [Fact]
        public async Task Completing_more_then_once_should_only_write_once()
        {
            // arrange
            var connectionString = $"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared";
            await InitializeDatabase(connectionString);
            var database = new SqliteConnection(connectionString);

            // act
            using var sut = new ShortLivedDbTransaction(() => database);
            await sut.ExecuteAsync(new InsertRandomText());
            await sut.CompleteAsync();
            await sut.CompleteAsync();

            // assert
            var recordsFound = await _keepConnectionOpen.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM TextEntries");

            Assert.Equal(OneRecordShouldBeFound, recordsFound);
        }

        [Fact]
        public async Task Abort_then_complete_should_not_write_data()
        {
            // arrange
            var connectionString = $"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared";
            await InitializeDatabase(connectionString);
            var database = new SqliteConnection(connectionString);

            // act
            using var sut = new ShortLivedDbTransaction(() => database);
            await sut.ExecuteAsync(new InsertRandomText());
            await sut.AbortAsync();
            await sut.CompleteAsync();

            // assert
            var recordsFound = await _keepConnectionOpen.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM TextEntries");
            Assert.Equal(ZeroRecordsShouldBeFound, recordsFound);
        }

        [Fact]
        public async Task Query_should_not_fetch_non_written_data()
        {
            // arrange
            var connectionString = $"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared";
            await InitializeDatabase(connectionString);
            var database = new SqliteConnection(connectionString);

            // act
            using var sut = new ShortLivedDbTransaction(() => database);
            await sut.ExecuteAsync(new InsertRandomText()); // <-- insert record
            var recordCount = await sut.QueryAsync(new CountTextEntries());
            await sut.CompleteAsync();

            // Assert
            Assert.Equal(ZeroRecordsShouldBeFound, recordCount);
        }

        public async Task Completed_data_should_be_avaiable_for_query()
        {
            // arrange
            var connectionString = $"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared";
            await InitializeDatabase(connectionString);
            var database = new SqliteConnection(connectionString);

            // act
            using var sut = new ShortLivedDbTransaction(() => database);
            await sut.ExecuteAsync(new InsertRandomText()); // <-- insert record
            await sut.CompleteAsync();

            var recordCount = await sut.QueryAsync(new CountTextEntries());

            // Assert
            Assert.Equal(OneRecordShouldBeFound, recordCount);
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
    }
}
