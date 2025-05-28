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

using System.Data;
using System.Data.Common;
using Dapper;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;

namespace Energinet.DataHub.EDI.IntegrationTests.Infrastructure.DapperAndEf;

[Collection("IntegrationTest")]
public class DapperAndEfTests(IntegrationTestFixture fixture) : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture = fixture;

    public Task InitializeAsync() => _fixture.InitializeAsync();

    public Task DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public async Task Ef_SaveChangesAsync_will_commit_immediately_without_transaction()
    {
        // Arrange
        await using var connectionUsedForWriting = new SqlConnection(_fixture.DatabaseManager.ConnectionString);
        await using var connectionUsedForReading = new SqlConnection(_fixture.DatabaseManager.ConnectionString);
        await connectionUsedForWriting.OpenAsync();
        await connectionUsedForReading.OpenAsync();

        var options = new DbContextOptionsBuilder<SomeDomainEntityDbContext>()
            .UseSqlServer(connectionUsedForWriting)
            .Options;

        // Check if the table exists and create it if necessary
        var tableExists = await connectionUsedForWriting.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SomeDomainEntities'");

        if (tableExists == 0)
        {
            await connectionUsedForWriting.ExecuteAsync(
                "CREATE TABLE SomeDomainEntities (Id INT PRIMARY KEY, SomeTimeStamp DATETIMEOFFSET NULL)");
        }

        await connectionUsedForWriting.ExecuteAsync("TRUNCATE TABLE SomeDomainEntities");

        // Pre-condition: Ensure the table is empty before the test
        var preInsertionResult = await connectionUsedForReading.QueryAsync("SELECT * FROM SomeDomainEntities");
        Assert.Empty(preInsertionResult);

        // Act: Insert using EF
        await using var context = new SomeDomainEntityDbContext(options);
        var entity = new SomeDomainEntity { Id = 1 };
        context.SomeDomainEntities.Add(entity);
        await context.SaveChangesAsync();

        // Act: Query using Dapper
        var dapperResult =
            await connectionUsedForReading.QuerySingleAsync<SomeDomainEntity>("SELECT * FROM SomeDomainEntities");

        // Assert
        Assert.NotNull(dapperResult);
        Assert.Equal(1, dapperResult.Id);
    }

    [Fact]
    public async Task Ef_SaveChangesAsync_will_not_commit_until_transaction_is_committed()
    {
        // Arrange
        await using var connectionUsedForWriting = new SqlConnection(_fixture.DatabaseManager.ConnectionString);
        await using var connectionUsedForReading = new SqlConnection(_fixture.DatabaseManager.ConnectionString);
        await connectionUsedForWriting.OpenAsync();
        await connectionUsedForReading.OpenAsync();

        var options = new DbContextOptionsBuilder<SomeDomainEntityDbContext>()
            .UseSqlServer(connectionUsedForWriting)
            .Options;

        // Check if the table exists and create it if necessary
        var tableExists = await connectionUsedForWriting.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SomeDomainEntities'");

        if (tableExists == 0)
        {
            await connectionUsedForWriting.ExecuteAsync(
                "CREATE TABLE SomeDomainEntities (Id INT PRIMARY KEY, SomeTimeStamp DATETIMEOFFSET NULL)");
        }

        await connectionUsedForWriting.ExecuteAsync("TRUNCATE TABLE SomeDomainEntities");

        // Pre-condition: Ensure the table is empty before the test
        var preInsertionResult = await connectionUsedForReading.QueryAsync("SELECT * FROM SomeDomainEntities");
        Assert.Empty(preInsertionResult);

        // Act: Insert using EF using a transaction
        await using var transaction =
            await connectionUsedForWriting.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await using var context = new SomeDomainEntityDbContext(options);
        await context.Database.UseTransactionAsync(transaction);

        var entity = new SomeDomainEntity { Id = 1 };
        context.SomeDomainEntities.Add(entity);
        await context.SaveChangesAsync();

        // Act: Query using Dapper
        var dapperResult =
            await connectionUsedForReading.QueryAsync<SomeDomainEntity>("SELECT * FROM SomeDomainEntities");

        Assert.Empty(dapperResult);

        // Commit the transaction
        await transaction.CommitAsync();

        // Re-query using Dapper after committing the transaction
        dapperResult = (await connectionUsedForReading.QueryAsync<SomeDomainEntity>("SELECT * FROM SomeDomainEntities"))
            .ToList();

        // Assert
        Assert.NotNull(dapperResult);
        Assert.Single(dapperResult);
        Assert.Equal(1, dapperResult.Single().Id);
    }

    [Fact]
    public async Task Ef_and_Dapper_can_share_connection_and_transaction_made_by_dapper()
    {
        // Arrange
        await using var connectionUsedForWriting = new SqlConnection(_fixture.DatabaseManager.ConnectionString);
        await using var connectionUsedForReading = new SqlConnection(_fixture.DatabaseManager.ConnectionString);
        await connectionUsedForWriting.OpenAsync();
        await connectionUsedForReading.OpenAsync();

        var options = new DbContextOptionsBuilder<SomeDomainEntityDbContext>()
            .UseSqlServer(connectionUsedForWriting)
            .Options;

        // Check if the table exists and create it if necessary
        var tableExists = await connectionUsedForWriting.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SomeDomainEntities'");

        if (tableExists == 0)
        {
            await connectionUsedForWriting.ExecuteAsync(
                "CREATE TABLE SomeDomainEntities (Id INT PRIMARY KEY, SomeTimeStamp DATETIMEOFFSET NULL)");
        }

        await connectionUsedForWriting.ExecuteAsync("TRUNCATE TABLE SomeDomainEntities");

        // Pre-condition: Ensure the table is empty before the test
        var preInsertionResult = await connectionUsedForReading.QueryAsync("SELECT * FROM SomeDomainEntities");
        Assert.Empty(preInsertionResult);

        // Act: Insert using EF using a transaction
        await using var transaction =
            await connectionUsedForWriting.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await using var context = new SomeDomainEntityDbContext(options);
        await context.Database.UseTransactionAsync(transaction);

        var entity = new SomeDomainEntity { Id = 1 };
        context.SomeDomainEntities.Add(entity);
        await context.SaveChangesAsync();

        // Act: Insert using Dapper with the same transaction
        await connectionUsedForWriting.ExecuteScalarAsync(
            "INSERT INTO SomeDomainEntities (Id) VALUES (@Id)",
            new { Id = 2 },
            transaction: transaction);

        // Act: Query using Dapper
        var dapperResult =
            await connectionUsedForReading.QueryAsync<SomeDomainEntity>("SELECT * FROM SomeDomainEntities");

        Assert.Empty(dapperResult);

        // Commit the transaction
        await transaction.CommitAsync();

        // Re-query using Dapper after committing the transaction
        dapperResult = (await connectionUsedForReading.QueryAsync<SomeDomainEntity>("SELECT * FROM SomeDomainEntities"))
            .ToList();

        // Assert
        Assert.NotNull(dapperResult);
        Assert.Equal(2, dapperResult.Count());
        Assert.Equal([1, 2], [.. dapperResult.Select(e => e.Id).Order()]);
    }

    [Fact]
    public async Task Ef_and_Dapper_can_share_connection_and_transaction_made_by_ef()
    {
        // Arrange
        await using var connectionUsedForWriting = new SqlConnection(_fixture.DatabaseManager.ConnectionString);
        await using var connectionUsedForReading = new SqlConnection(_fixture.DatabaseManager.ConnectionString);
        await connectionUsedForWriting.OpenAsync();
        await connectionUsedForReading.OpenAsync();

        var options = new DbContextOptionsBuilder<SomeDomainEntityDbContext>()
            .UseSqlServer(connectionUsedForWriting)
            .Options;

        // Check if the table exists and create it if necessary
        var tableExists = await connectionUsedForWriting.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SomeDomainEntities'");

        if (tableExists == 0)
        {
            await connectionUsedForWriting.ExecuteAsync(
                "CREATE TABLE SomeDomainEntities (Id INT PRIMARY KEY, SomeTimeStamp DATETIMEOFFSET NULL)");
        }

        await connectionUsedForWriting.ExecuteAsync("TRUNCATE TABLE SomeDomainEntities");

        // Pre-condition: Ensure the table is empty before the test
        var preInsertionResult = await connectionUsedForReading.QueryAsync("SELECT * FROM SomeDomainEntities");
        Assert.Empty(preInsertionResult);

        // Act: Insert using EF using a transaction
        await using var context = new SomeDomainEntityDbContext(options);
        var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);

        var entity = new SomeDomainEntity { Id = 1 };
        context.SomeDomainEntities.Add(entity);
        await context.SaveChangesAsync();

        // Act: Insert using Dapper with the same transaction
        await connectionUsedForWriting.ExecuteScalarAsync(
            "INSERT INTO SomeDomainEntities (Id) VALUES (@Id)",
            new { Id = 2 },
            transaction: transaction.GetDbTransaction());

        // Act: Query using Dapper
        var dapperResult =
            await connectionUsedForReading.QueryAsync<SomeDomainEntity>("SELECT * FROM SomeDomainEntities");

        Assert.Empty(dapperResult);

        // Commit the transaction
        await transaction.CommitAsync();

        // Re-query using Dapper after committing the transaction
        dapperResult = (await connectionUsedForReading.QueryAsync<SomeDomainEntity>("SELECT * FROM SomeDomainEntities"))
            .ToList();

        // Assert
        Assert.NotNull(dapperResult);
        Assert.Equal(2, dapperResult.Count());
        Assert.Equal([1, 2], [.. dapperResult.Select(e => e.Id).Order()]);
    }

    [Fact]
    public async Task ReadCommitted_transactions_prevents_duplicate_insertions_from_outside_of_the_transaction()
    {
        // Arrange
        await using var connectionUsedForWriting = new SqlConnection(_fixture.DatabaseManager.ConnectionString);
        await using var connectionUsedForReading = new SqlConnection(_fixture.DatabaseManager.ConnectionString);
        await using var connectionUsedForInsertingDuplicate =
            new SqlConnection(_fixture.DatabaseManager.ConnectionString);

        await connectionUsedForWriting.OpenAsync();
        await connectionUsedForReading.OpenAsync();
        await connectionUsedForInsertingDuplicate.OpenAsync();

        var options = new DbContextOptionsBuilder<SomeDomainEntityDbContext>()
            .UseSqlServer(connectionUsedForWriting)
            .Options;

        // Check if the table exists and create it if necessary
        var tableExists = await connectionUsedForWriting.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SomeDomainEntities'");

        if (tableExists == 0)
        {
            await connectionUsedForWriting.ExecuteAsync(
                "CREATE TABLE SomeDomainEntities (Id INT PRIMARY KEY, SomeTimeStamp DATETIMEOFFSET NULL)");
        }

        await connectionUsedForWriting.ExecuteAsync("TRUNCATE TABLE SomeDomainEntities");

        // Pre-condition: Ensure the table is empty before the test
        var preInsertionResult = await connectionUsedForReading.QueryAsync("SELECT * FROM SomeDomainEntities");
        Assert.Empty(preInsertionResult);

        // Act: Insert using EF using a transaction
        await using var transaction =
            await connectionUsedForWriting.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await using var context = new SomeDomainEntityDbContext(options);
        await context.Database.UseTransactionAsync(transaction);

        var entity = new SomeDomainEntity { Id = 1 };
        context.SomeDomainEntities.Add(entity);
        await context.SaveChangesAsync();

        // Act: Insert using Dapper with the same transaction
        await connectionUsedForWriting.ExecuteScalarAsync(
            "INSERT INTO SomeDomainEntities (Id) VALUES (@Id)",
            new { Id = 2 },
            transaction: transaction);

        // Act: Insert a duplicate without a transaction
        var act = async () =>
            await connectionUsedForInsertingDuplicate.ExecuteAsync(
                "INSERT INTO SomeDomainEntities (Id) VALUES (@Id)",
                new { Id = 1 });

        await act.Should()
            .ThrowExactlyAsync<SqlException>()
            .WithMessage(
                "*The timeout period elapsed prior to completion of the operation or the server is not responding*");
    }

    [Fact]
    public async Task Can_read_and_write_to_multiple_entities_using_different_transactions()
    {
        // Arrange
        await using var connectionForSetup = new SqlConnection(_fixture.DatabaseManager.ConnectionString);
        await connectionForSetup.OpenAsync();

        // Check if the table exists and create it if necessary
        var tableExists = await connectionForSetup.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SomeDomainEntities'");

        if (tableExists == 0)
        {
            await connectionForSetup.ExecuteAsync(
                "CREATE TABLE SomeDomainEntities (Id INT PRIMARY KEY, SomeTimeStamp DATETIMEOFFSET NULL)");
        }

        await connectionForSetup.ExecuteAsync("TRUNCATE TABLE SomeDomainEntities");

        // Pre-condition: Ensure the table is empty before the test
        var preInsertionResult = await connectionForSetup.QueryAsync("SELECT * FROM SomeDomainEntities");
        Assert.Empty(preInsertionResult);

        // Act: Insert using EF using a transaction
        var insertEntities = Enumerable.Range(1, 10)
            .Select(async id =>
            {
                await using var connectio = new SqlConnection(_fixture.DatabaseManager.ConnectionString);
                await connectio.OpenAsync();

                var options = new DbContextOptionsBuilder<SomeDomainEntityDbContext>()
                    .UseSqlServer(connectio)
                    .Options;

                await using var transaction = await connectio.BeginTransactionAsync(IsolationLevel.ReadCommitted);
                await using var context = new SomeDomainEntityDbContext(options);
                await context.Database.UseTransactionAsync(transaction);

                var entity = new SomeDomainEntity { Id = id };
                context.SomeDomainEntities.Add(entity);
                await context.SaveChangesAsync();

                // Commit the transaction
                await transaction.CommitAsync();
            });

        await Task.WhenAll(insertEntities);

        // Act: Read and update entities using multiple transactions
        var transactionList = new List<DbTransaction>();
        var dbContextList = new List<SomeDomainEntityDbContext>();

        var readAndUpdateEntities = Enumerable.Range(1, 10)
            .Select(async id =>
            {
                var connection = new SqlConnection(_fixture.DatabaseManager.ConnectionString);
                await connection.OpenAsync();

                var options = new DbContextOptionsBuilder<SomeDomainEntityDbContext>()
                    .UseSqlServer(connection)
                    .Options;

                var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted);
                var context = new SomeDomainEntityDbContext(options);
                await context.Database.UseTransactionAsync(transaction);

                transactionList.Add(transaction);
                dbContextList.Add(context);

                var entity = await context.SomeDomainEntities.FindAsync(id);
                if (entity != null)
                {
                    entity.SomeTimeStamp = DateTimeOffset.UtcNow;
                    await context.SaveChangesAsync();
                }
            });

        await Task.WhenAll(readAndUpdateEntities);

        // Act: Commit all transactions
        await Task.WhenAll(transactionList.Select(t => t.CommitAsync()));

        // Act: Dispose transactions and contexts
        foreach (var context in dbContextList)
        {
            await context.DisposeAsync();
        }

        foreach (var transaction in transactionList)
        {
            await transaction.DisposeAsync();
        }

        // Act: Query using Dapper
        await using var connectionForReading = new SqlConnection(_fixture.DatabaseManager.ConnectionString);
        await connectionForReading.OpenAsync();
        var dapperResult =
            (await connectionForReading.QueryAsync<SomeDomainEntity>("SELECT * FROM SomeDomainEntities")).ToList();

        // Assert
        Assert.NotNull(dapperResult);
        Assert.Equal(10, dapperResult.Count);
        Assert.Equal(
            [1, 2, 3, 4, 5, 6, 7, 8, 9, 10],
            [.. dapperResult.Select(e => e.Id).Order()]);

        Assert.All(dapperResult, e => e.SomeTimeStamp.Should().NotBeNull());
    }

    public class SomeDomainEntity
    {
        public int Id { get; set; }

        public DateTimeOffset? SomeTimeStamp { get; set; }
    }

    public class SomeDomainEntityDbContext(DbContextOptions<SomeDomainEntityDbContext> options) : DbContext(options)
    {
        public DbSet<SomeDomainEntity> SomeDomainEntities { get; set; } = null!;
    }
}
