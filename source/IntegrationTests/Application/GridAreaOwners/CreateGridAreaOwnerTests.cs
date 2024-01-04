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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.IntegrationTests.Application.Transactions.Aggregations;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.MasterData.Interfaces.Models;
using Xunit;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.GridAreaOwners;

public sealed class CreateGridAreaOwnerTests : TestBase
{
    private readonly IMasterDataClient _masterDataClient;
    private readonly IDatabaseConnectionFactory _connectionFactory;
    private readonly IUnitOfWork _unitOfWork;

    public CreateGridAreaOwnerTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _masterDataClient = GetService<IMasterDataClient>();
        _connectionFactory = GetService<IDatabaseConnectionFactory>();
        _unitOfWork = GetService<IUnitOfWork>();
    }

    [Fact]
    public async Task Grid_area_owner_is_created()
    {
        var gridAreaOwnershipAssignedDto = CreateGridAreaOwnershipAssignedDto();

        await _masterDataClient.UpdateGridAreaOwnershipAsync(gridAreaOwnershipAssignedDto, CancellationToken.None);
        await _unitOfWork.CommitTransactionAsync();

        var gridAreaOwner = (await GetGridAreaOwners()).ToList();

        Assert.Single(gridAreaOwner);
        Assert.Equal(SampleData.GridAreaCode, gridAreaOwner.First().GridAreaCode);
        Assert.Equal(SampleData.StartOfPeriod.ToDateTimeUtc(), gridAreaOwner.First().ValidFrom);
        Assert.Equal(SampleData.GridOperatorNumber.Value, gridAreaOwner.First().GridAreaOwnerActorNumber);
        Assert.Equal(42, gridAreaOwner.First().SequenceNumber);
    }

    [Fact]
    public async Task No_duplicate_or_consistency_checks_enforced_when_storing_grid_area_owners_with_single_commit()
    {
        var gridAreaOwnershipAssignedDto1 = CreateGridAreaOwnershipAssignedDto();
        var gridAreaOwnershipAssignedDto2 = CreateGridAreaOwnershipAssignedDto();
        var gridAreaOwnershipAssignedDto3 = new GridAreaOwnershipAssignedDto(
            SampleData.GridAreaCode,
            SampleData.EndOfPeriod,
            SampleData.BalanceResponsibleNumber,
            42);

        var gridAreaOwnershipAssignedDto4 = new GridAreaOwnershipAssignedDto(
            SampleData.GridAreaCode,
            SampleData.StartOfPeriod,
            SampleData.EnergySupplierNumber,
            42);

        var gridAreaOwnershipAssignedDto5 = new GridAreaOwnershipAssignedDto(
            SampleData.GridAreaCode,
            SampleData.EndOfPeriod,
            SampleData.EnergySupplierNumber2,
            42);

        await _masterDataClient.UpdateGridAreaOwnershipAsync(gridAreaOwnershipAssignedDto1, CancellationToken.None);
        await _masterDataClient.UpdateGridAreaOwnershipAsync(gridAreaOwnershipAssignedDto2, CancellationToken.None);
        await _masterDataClient.UpdateGridAreaOwnershipAsync(gridAreaOwnershipAssignedDto3, CancellationToken.None);
        await _masterDataClient.UpdateGridAreaOwnershipAsync(gridAreaOwnershipAssignedDto4, CancellationToken.None);
        await _masterDataClient.UpdateGridAreaOwnershipAsync(gridAreaOwnershipAssignedDto5, CancellationToken.None);

        await _unitOfWork.CommitTransactionAsync();

        var gridAreaOwner = (await GetGridAreaOwners()).ToList();

        Assert.Equal(5, gridAreaOwner.Count);
        Assert.Fail();
        Assert.Equal(SampleData.GridAreaCode, gridAreaOwner.First().GridAreaCode);
        Assert.Equal(SampleData.StartOfPeriod.ToDateTimeUtc(), gridAreaOwner.First().ValidFrom);
        Assert.Equal(SampleData.GridOperatorNumber.Value, gridAreaOwner.First().GridAreaOwnerActorNumber);
        Assert.Equal(42, gridAreaOwner.First().SequenceNumber);
    }

    [Fact]
    public async Task No_duplicate_or_consistency_checks_enforced_when_storing_grid_area_owners_with_multiple_commits()
    {
        var gridAreaOwnershipAssignedDto1 = CreateGridAreaOwnershipAssignedDto();
        var gridAreaOwnershipAssignedDto2 = CreateGridAreaOwnershipAssignedDto();
        var gridAreaOwnershipAssignedDto3 = new GridAreaOwnershipAssignedDto(
            SampleData.GridAreaCode,
            SampleData.EndOfPeriod,
            SampleData.BalanceResponsibleNumber,
            42);

        var gridAreaOwnershipAssignedDto4 = new GridAreaOwnershipAssignedDto(
            SampleData.GridAreaCode,
            SampleData.StartOfPeriod,
            SampleData.EnergySupplierNumber,
            42);

        var gridAreaOwnershipAssignedDto5 = new GridAreaOwnershipAssignedDto(
            SampleData.GridAreaCode,
            SampleData.EndOfPeriod,
            SampleData.EnergySupplierNumber2,
            42);

        await _masterDataClient.UpdateGridAreaOwnershipAsync(gridAreaOwnershipAssignedDto1, CancellationToken.None);
        await _unitOfWork.CommitTransactionAsync();

        await _masterDataClient.UpdateGridAreaOwnershipAsync(gridAreaOwnershipAssignedDto2, CancellationToken.None);
        await _unitOfWork.CommitTransactionAsync();

        await _masterDataClient.UpdateGridAreaOwnershipAsync(gridAreaOwnershipAssignedDto3, CancellationToken.None);
        await _unitOfWork.CommitTransactionAsync();

        await _masterDataClient.UpdateGridAreaOwnershipAsync(gridAreaOwnershipAssignedDto4, CancellationToken.None);
        await _unitOfWork.CommitTransactionAsync();

        await _masterDataClient.UpdateGridAreaOwnershipAsync(gridAreaOwnershipAssignedDto5, CancellationToken.None);
        await _unitOfWork.CommitTransactionAsync();

        var gridAreaOwner = (await GetGridAreaOwners()).ToList();

        Assert.Equal(5, gridAreaOwner.Count);
        Assert.Fail();
        Assert.Equal(SampleData.GridAreaCode, gridAreaOwner.First().GridAreaCode);
        Assert.Equal(SampleData.StartOfPeriod.ToDateTimeUtc(), gridAreaOwner.First().ValidFrom);
        Assert.Equal(SampleData.GridOperatorNumber.Value, gridAreaOwner.First().GridAreaOwnerActorNumber);
        Assert.Equal(42, gridAreaOwner.First().SequenceNumber);
    }

    private static GridAreaOwnershipAssignedDto CreateGridAreaOwnershipAssignedDto()
    {
        return new GridAreaOwnershipAssignedDto(
            SampleData.GridAreaCode,
            SampleData.StartOfPeriod,
            SampleData.GridOperatorNumber,
            42);
    }

    private async Task<IEnumerable<GridAreaOwner>> GetGridAreaOwners()
    {
        using var connection = await _connectionFactory.GetConnectionAndOpenAsync(CancellationToken.None);
        var sql = $"SELECT GridAreaCode, ValidFrom, GridAreaOwnerActorNumber, SequenceNumber " +
                  $"FROM [dbo].[GridAreaOwner] " +
                  $"WHERE GridAreaCode = '{SampleData.GridAreaCode}'";

        return await connection.QueryAsync<GridAreaOwner>(sql);
    }

    private sealed record GridAreaOwner(
        string GridAreaCode,
        DateTime ValidFrom,
        string GridAreaOwnerActorNumber,
        int SequenceNumber);
}
