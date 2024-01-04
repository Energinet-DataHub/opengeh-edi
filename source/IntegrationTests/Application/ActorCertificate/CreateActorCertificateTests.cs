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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.IntegrationTests.Application.Transactions.Aggregations;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.MasterData.Interfaces.Models;
using Xunit;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.ActorCertificate;

public class CreateActorCertificateTests : TestBase
{
    private readonly IMasterDataClient _masterDataClient;
    private readonly IDatabaseConnectionFactory _connectionFactory;
    private readonly IUnitOfWork _unitOfWork;

    public CreateActorCertificateTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _masterDataClient = GetService<IMasterDataClient>();
        _connectionFactory = GetService<IDatabaseConnectionFactory>();
        _unitOfWork = GetService<IUnitOfWork>();
    }

    [Fact]
    public async Task Actor_certificate_is_created()
    {
        var actorCertificateCredentialsAssignedDto = CreateActorCertificateCredentialsAssignedDto();

        await _masterDataClient.CreateOrUpdateActorCertificateAsync(
            actorCertificateCredentialsAssignedDto,
            CancellationToken.None);

        await _unitOfWork.CommitTransactionAsync();

        var actorCertificates = (await GetActorCertificates()).ToList();

        Assert.Single(actorCertificates);
        Assert.Equal(SampleData.GridOperatorNumber.Value, actorCertificates.First().ActorNumber);
        Assert.Equal(MarketRole.GridOperator.Code, actorCertificates.First().ActorRole);
        Assert.Equal("ThisIsThumbprint", actorCertificates.First().Thumbprint);
        Assert.Equal(SampleData.StartOfPeriod.ToDateTimeUtc(), actorCertificates.First().ValidFrom);
        Assert.Equal(42, actorCertificates.First().SequenceNumber);
    }

    [Fact]
    public async Task Actor_certificate_is_not_created_multiple_times_with_single_commit()
    {
        var actorCertificateCredentialsAssignedDto1 = CreateActorCertificateCredentialsAssignedDto();
        var actorCertificateCredentialsAssignedDto2 = CreateActorCertificateCredentialsAssignedDto();
        var actorCertificateCredentialsAssignedDto3 = CreateActorCertificateCredentialsAssignedDto();
        var actorCertificateCredentialsAssignedDto4 = CreateActorCertificateCredentialsAssignedDto();

        await _masterDataClient.CreateOrUpdateActorCertificateAsync(
            actorCertificateCredentialsAssignedDto1,
            CancellationToken.None);

        await _masterDataClient.CreateOrUpdateActorCertificateAsync(
            actorCertificateCredentialsAssignedDto2,
            CancellationToken.None);

        await _masterDataClient.CreateOrUpdateActorCertificateAsync(
            actorCertificateCredentialsAssignedDto3,
            CancellationToken.None);

        await _masterDataClient.CreateOrUpdateActorCertificateAsync(
            actorCertificateCredentialsAssignedDto4,
            CancellationToken.None);

        await _unitOfWork.CommitTransactionAsync();

        var actorCertificates = (await GetActorCertificates()).ToList();

        Assert.Single(actorCertificates);
        Assert.Equal(SampleData.GridOperatorNumber.Value, actorCertificates.First().ActorNumber);
        Assert.Equal(MarketRole.GridOperator.Code, actorCertificates.First().ActorRole);
        Assert.Equal("ThisIsThumbprint", actorCertificates.First().Thumbprint);
        Assert.Equal(SampleData.StartOfPeriod.ToDateTimeUtc(), actorCertificates.First().ValidFrom);
        Assert.Equal(42, actorCertificates.First().SequenceNumber);
    }

    [Fact]
    public async Task Actor_certificate_is_not_created_multiple_times_with_multiple_commits()
    {
        var actorCertificateCredentialsAssignedDto1 = CreateActorCertificateCredentialsAssignedDto();
        var actorCertificateCredentialsAssignedDto2 = CreateActorCertificateCredentialsAssignedDto();
        var actorCertificateCredentialsAssignedDto3 = CreateActorCertificateCredentialsAssignedDto();
        var actorCertificateCredentialsAssignedDto4 = CreateActorCertificateCredentialsAssignedDto();

        await _masterDataClient.CreateOrUpdateActorCertificateAsync(
            actorCertificateCredentialsAssignedDto1,
            CancellationToken.None);

        await _unitOfWork.CommitTransactionAsync();

        await _masterDataClient.CreateOrUpdateActorCertificateAsync(
            actorCertificateCredentialsAssignedDto2,
            CancellationToken.None);

        await _unitOfWork.CommitTransactionAsync();

        await _masterDataClient.CreateOrUpdateActorCertificateAsync(
            actorCertificateCredentialsAssignedDto3,
            CancellationToken.None);

        await _unitOfWork.CommitTransactionAsync();

        await _masterDataClient.CreateOrUpdateActorCertificateAsync(
            actorCertificateCredentialsAssignedDto4,
            CancellationToken.None);

        await _unitOfWork.CommitTransactionAsync();

        var actorCertificates = (await GetActorCertificates()).ToList();

        Assert.Single(actorCertificates);
        Assert.Equal(SampleData.GridOperatorNumber.Value, actorCertificates.First().ActorNumber);
        Assert.Equal(MarketRole.GridOperator.Code, actorCertificates.First().ActorRole);
        Assert.Equal("ThisIsThumbprint", actorCertificates.First().Thumbprint);
        Assert.Equal(SampleData.StartOfPeriod.ToDateTimeUtc(), actorCertificates.First().ValidFrom);
        Assert.Equal(42, actorCertificates.First().SequenceNumber);
    }

    private static ActorCertificateCredentialsAssignedDto CreateActorCertificateCredentialsAssignedDto()
    {
        return new ActorCertificateCredentialsAssignedDto(
            SampleData.GridOperatorNumber,
            MarketRole.GridOperator,
            new CertificateThumbprintDto("ThisIsThumbprint"),
            SampleData.StartOfPeriod,
            42);
    }

    private async Task<IEnumerable<ActorCertificate>> GetActorCertificates()
    {
        using var connection = await _connectionFactory.GetConnectionAndOpenAsync(CancellationToken.None);
        var sql = $"SELECT ActorNumber, ActorRole, Thumbprint, ValidFrom, SequenceNumber " +
                  $"FROM [dbo].[ActorCertificate] ";

        return await connection.QueryAsync<ActorCertificate>(sql);
    }

    private sealed record ActorCertificate(
        string ActorNumber,
        string ActorRole,
        string Thumbprint,
        DateTime ValidFrom,
        int SequenceNumber);
}
