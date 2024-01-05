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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.Common.DateTime;
using Energinet.DataHub.EDI.IntegrationTests.Factories;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.IntegrationTests.TestDoubles;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.MasterData.Interfaces.Models;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Xunit;

namespace Energinet.DataHub.EDI.IntegrationTests.MasterData;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Test class")]
public abstract class MasterDataClientTests : TestBase
{
    private readonly IMasterDataClient _masterDataClient;
    private readonly IDatabaseConnectionFactory _connectionFactory;
    private readonly IUnitOfWork _unitOfWork;

    private MasterDataClientTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _masterDataClient = GetService<IMasterDataClient>();
        _connectionFactory = GetService<IDatabaseConnectionFactory>();
        _unitOfWork = GetService<IUnitOfWork>();
    }

    public abstract class ActorsTests : MasterDataClientTests
    {
        private ActorsTests(DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
        }

        private static CreateActorDto CreateDto()
        {
            return new CreateActorDto(SampleData.ExternalId, ActorNumber.Create(SampleData.SomeActorNumber));
        }

        private async Task<IEnumerable<Actor>> GetActors()
        {
            using var connection = await _connectionFactory.GetConnectionAndOpenAsync(CancellationToken.None);
            var sql =
                $"SELECT Id, ActorNumber, ExternalId " +
                $"FROM [dbo].[Actor]";

            return await connection.QueryAsync<Actor>(sql);
        }

        public sealed class CreateActorIfNotExistAsyncTests : ActorsTests
        {
            public CreateActorIfNotExistAsyncTests(DatabaseFixture databaseFixture)
                : base(databaseFixture)
            {
            }

            [Fact]
            public async Task Actor_is_created()
            {
                var createActorDto = CreateDto();

                await _masterDataClient.CreateActorIfNotExistAsync(createActorDto, CancellationToken.None);
                await _unitOfWork.CommitTransactionAsync();

                var actors = (await GetActors()).ToList();

                actors.Should().ContainSingle();
                var actor = actors.First();
                actor.ActorNumber.Should().Be(SampleData.SomeActorNumber);
                actor.ExternalId.Should().Be(SampleData.ExternalId);
            }

            [Fact]
            public async Task Actor_is_not_created_multiple_times_with_single_commit()
            {
                var createActorDto1 = CreateDto();
                var createActorDto2 = CreateDto();
                var createActorDto3 = CreateDto();
                var createActorDto4 = CreateDto();

                await _masterDataClient.CreateActorIfNotExistAsync(createActorDto1, CancellationToken.None);
                await _masterDataClient.CreateActorIfNotExistAsync(createActorDto2, CancellationToken.None);
                await _masterDataClient.CreateActorIfNotExistAsync(createActorDto3, CancellationToken.None);
                await _masterDataClient.CreateActorIfNotExistAsync(createActorDto4, CancellationToken.None);

                await _unitOfWork.CommitTransactionAsync();

                var actors = (await GetActors()).ToList();

                actors.Should().ContainSingle();
                var actor = actors.First();
                actor.ActorNumber.Should().Be(SampleData.SomeActorNumber);
                actor.ExternalId.Should().Be(SampleData.ExternalId);
            }

            [Fact]
            public async Task Actor_is_not_created_multiple_times_with_multiple_commits()
            {
                var createActorDto1 = CreateDto();
                var createActorDto2 = CreateDto();
                var createActorDto3 = CreateDto();
                var createActorDto4 = CreateDto();

                await _masterDataClient.CreateActorIfNotExistAsync(createActorDto1, CancellationToken.None);
                await _unitOfWork.CommitTransactionAsync();
                await _masterDataClient.CreateActorIfNotExistAsync(createActorDto2, CancellationToken.None);
                await _unitOfWork.CommitTransactionAsync();
                await _masterDataClient.CreateActorIfNotExistAsync(createActorDto3, CancellationToken.None);
                await _unitOfWork.CommitTransactionAsync();
                await _masterDataClient.CreateActorIfNotExistAsync(createActorDto4, CancellationToken.None);
                await _unitOfWork.CommitTransactionAsync();

                var actors = (await GetActors()).ToList();

                actors.Should().ContainSingle();
                var actor = actors.First();
                actor.ActorNumber.Should().Be(SampleData.SomeActorNumber);
                actor.ExternalId.Should().Be(SampleData.ExternalId);
            }
        }

        public sealed class GetActorNumberByExternalIdAsync : ActorsTests
        {
            public GetActorNumberByExternalIdAsync(DatabaseFixture databaseFixture)
                : base(databaseFixture)
            {
            }

            [Fact]
            public async Task Requesting_an_actor_number_with_an_empty_db_gives_null_result()
            {
                var result = await _masterDataClient.GetActorNumberByExternalIdAsync(
                    SampleData.ExternalId,
                    CancellationToken.None);

                result.Should().BeNull();
            }

            [Fact]
            public async Task Requesting_an_actor_number_for_an_unknown_external_id_gives_null_result()
            {
                var createActorDto = CreateDto();

                await _masterDataClient.CreateActorIfNotExistAsync(createActorDto, CancellationToken.None);
                await _unitOfWork.CommitTransactionAsync();

                (await GetActors()).ToList().Should().NotBeNullOrEmpty();

                var result = await _masterDataClient.GetActorNumberByExternalIdAsync(
                    Guid.Parse("2ad87ee6-730e-4c2e-ba95-b220b1b7953d").ToString(),
                    CancellationToken.None);

                result.Should().BeNull();
            }

            [Fact]
            public async Task Requesting_an_actor_number_for_a_known_external_id_gives_an_actor_number()
            {
                var createActorDto = CreateDto();

                await _masterDataClient.CreateActorIfNotExistAsync(createActorDto, CancellationToken.None);
                await _unitOfWork.CommitTransactionAsync();

                (await GetActors()).ToList().Should().NotBeNullOrEmpty();

                var result = await _masterDataClient.GetActorNumberByExternalIdAsync(
                    SampleData.ExternalId,
                    CancellationToken.None);

                result.Should().Be(ActorNumber.Create(SampleData.SomeActorNumber));
            }

            [Fact]
            public async Task Requesting_an_actor_number_for_a_duplicated_external_id_gives_a_random_actor_number()
            {
                var createActorDto1 = new CreateActorDto(SampleData.ExternalId, SampleData.BalanceResponsibleNumber);
                var createActorDto2 = new CreateActorDto(SampleData.ExternalId, SampleData.GridOperatorNumber);

                await _masterDataClient.CreateActorIfNotExistAsync(createActorDto1, CancellationToken.None);
                await _masterDataClient.CreateActorIfNotExistAsync(createActorDto2, CancellationToken.None);
                await _unitOfWork.CommitTransactionAsync();

                (await GetActors()).ToList().Should().HaveCount(2);

                var result = await _masterDataClient.GetActorNumberByExternalIdAsync(
                    SampleData.ExternalId,
                    CancellationToken.None);

                result.Should().BeOneOf(SampleData.BalanceResponsibleNumber, SampleData.GridOperatorNumber);
            }
        }

        private sealed record Actor(Guid Id, string ActorNumber, string ExternalId);
    }

    public abstract class GridAreaOwnershipTests : MasterDataClientTests
    {
        private GridAreaOwnershipTests(DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
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
                      $"FROM [dbo].[GridAreaOwner]";

            return await connection.QueryAsync<GridAreaOwner>(sql);
        }

        public sealed class UpdateGridAreaOwnershipAsync : GridAreaOwnershipTests
        {
            public UpdateGridAreaOwnershipAsync(DatabaseFixture databaseFixture)
                : base(databaseFixture)
            {
            }

            [Fact]
            public async Task Grid_area_owner_is_created()
            {
                var gridAreaOwnershipAssignedDto = CreateGridAreaOwnershipAssignedDto();

                await _masterDataClient.UpdateGridAreaOwnershipAsync(
                    gridAreaOwnershipAssignedDto,
                    CancellationToken.None);
                await _unitOfWork.CommitTransactionAsync();

                var gridAreaOwners = (await GetGridAreaOwners()).ToList();

                gridAreaOwners.Should().ContainSingle();
                var gridAreaOwner = gridAreaOwners.First();
                gridAreaOwner.GridAreaCode.Should().Be(SampleData.GridAreaCode);
                gridAreaOwner.ValidFrom.Should().Be(SampleData.StartOfPeriod.ToDateTimeUtc());
                gridAreaOwner.GridAreaOwnerActorNumber.Should().Be(SampleData.GridOperatorNumber.Value);
                gridAreaOwner.SequenceNumber.Should().Be(42);
            }

            [Fact]
            public async Task
                No_duplicate_or_consistency_checks_enforced_when_storing_grid_area_owners_with_single_commit()
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

                await _masterDataClient.UpdateGridAreaOwnershipAsync(
                    gridAreaOwnershipAssignedDto1,
                    CancellationToken.None);
                await _masterDataClient.UpdateGridAreaOwnershipAsync(
                    gridAreaOwnershipAssignedDto2,
                    CancellationToken.None);
                await _masterDataClient.UpdateGridAreaOwnershipAsync(
                    gridAreaOwnershipAssignedDto3,
                    CancellationToken.None);
                await _masterDataClient.UpdateGridAreaOwnershipAsync(
                    gridAreaOwnershipAssignedDto4,
                    CancellationToken.None);
                await _masterDataClient.UpdateGridAreaOwnershipAsync(
                    gridAreaOwnershipAssignedDto5,
                    CancellationToken.None);

                await _unitOfWork.CommitTransactionAsync();

                var gridAreaOwners = (await GetGridAreaOwners()).ToList();

                gridAreaOwners.Should().HaveCount(5);
                gridAreaOwners.Should()
                    .Satisfy(
                        gao =>
                            gao.GridAreaCode == gridAreaOwnershipAssignedDto1.GridAreaCode
                            && gao.ValidFrom == gridAreaOwnershipAssignedDto1.ValidFrom.ToDateTimeUtc()
                            && gao.GridAreaOwnerActorNumber == gridAreaOwnershipAssignedDto1.GridAreaOwner.Value
                            && gao.SequenceNumber == gridAreaOwnershipAssignedDto1.SequenceNumber,
                        gao =>
                            gao.GridAreaCode == gridAreaOwnershipAssignedDto2.GridAreaCode
                            && gao.ValidFrom == gridAreaOwnershipAssignedDto2.ValidFrom.ToDateTimeUtc()
                            && gao.GridAreaOwnerActorNumber == gridAreaOwnershipAssignedDto2.GridAreaOwner.Value
                            && gao.SequenceNumber == gridAreaOwnershipAssignedDto2.SequenceNumber,
                        gao =>
                            gao.GridAreaCode == gridAreaOwnershipAssignedDto3.GridAreaCode
                            && gao.ValidFrom == gridAreaOwnershipAssignedDto3.ValidFrom.ToDateTimeUtc()
                            && gao.GridAreaOwnerActorNumber == gridAreaOwnershipAssignedDto3.GridAreaOwner.Value
                            && gao.SequenceNumber == gridAreaOwnershipAssignedDto3.SequenceNumber,
                        gao =>
                            gao.GridAreaCode == gridAreaOwnershipAssignedDto4.GridAreaCode
                            && gao.ValidFrom == gridAreaOwnershipAssignedDto4.ValidFrom.ToDateTimeUtc()
                            && gao.GridAreaOwnerActorNumber == gridAreaOwnershipAssignedDto4.GridAreaOwner.Value
                            && gao.SequenceNumber == gridAreaOwnershipAssignedDto4.SequenceNumber,
                        gao =>
                            gao.GridAreaCode == gridAreaOwnershipAssignedDto5.GridAreaCode
                            && gao.ValidFrom == gridAreaOwnershipAssignedDto5.ValidFrom.ToDateTimeUtc()
                            && gao.GridAreaOwnerActorNumber == gridAreaOwnershipAssignedDto5.GridAreaOwner.Value
                            && gao.SequenceNumber == gridAreaOwnershipAssignedDto5.SequenceNumber);
            }

            [Fact]
            public async Task
                No_duplicate_or_consistency_checks_enforced_when_storing_grid_area_owners_with_multiple_commits()
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

                await _masterDataClient.UpdateGridAreaOwnershipAsync(
                    gridAreaOwnershipAssignedDto1,
                    CancellationToken.None);
                await _unitOfWork.CommitTransactionAsync();

                await _masterDataClient.UpdateGridAreaOwnershipAsync(
                    gridAreaOwnershipAssignedDto2,
                    CancellationToken.None);
                await _unitOfWork.CommitTransactionAsync();

                await _masterDataClient.UpdateGridAreaOwnershipAsync(
                    gridAreaOwnershipAssignedDto3,
                    CancellationToken.None);
                await _unitOfWork.CommitTransactionAsync();

                await _masterDataClient.UpdateGridAreaOwnershipAsync(
                    gridAreaOwnershipAssignedDto4,
                    CancellationToken.None);
                await _unitOfWork.CommitTransactionAsync();

                await _masterDataClient.UpdateGridAreaOwnershipAsync(
                    gridAreaOwnershipAssignedDto5,
                    CancellationToken.None);
                await _unitOfWork.CommitTransactionAsync();

                var gridAreaOwners = (await GetGridAreaOwners()).ToList();

                gridAreaOwners.Should().HaveCount(5);
                gridAreaOwners.Should()
                    .Satisfy(
                        gao =>
                            gao.GridAreaCode == gridAreaOwnershipAssignedDto1.GridAreaCode
                            && gao.ValidFrom == gridAreaOwnershipAssignedDto1.ValidFrom.ToDateTimeUtc()
                            && gao.GridAreaOwnerActorNumber == gridAreaOwnershipAssignedDto1.GridAreaOwner.Value
                            && gao.SequenceNumber == gridAreaOwnershipAssignedDto1.SequenceNumber,
                        gao =>
                            gao.GridAreaCode == gridAreaOwnershipAssignedDto2.GridAreaCode
                            && gao.ValidFrom == gridAreaOwnershipAssignedDto2.ValidFrom.ToDateTimeUtc()
                            && gao.GridAreaOwnerActorNumber == gridAreaOwnershipAssignedDto2.GridAreaOwner.Value
                            && gao.SequenceNumber == gridAreaOwnershipAssignedDto2.SequenceNumber,
                        gao =>
                            gao.GridAreaCode == gridAreaOwnershipAssignedDto3.GridAreaCode
                            && gao.ValidFrom == gridAreaOwnershipAssignedDto3.ValidFrom.ToDateTimeUtc()
                            && gao.GridAreaOwnerActorNumber == gridAreaOwnershipAssignedDto3.GridAreaOwner.Value
                            && gao.SequenceNumber == gridAreaOwnershipAssignedDto3.SequenceNumber,
                        gao =>
                            gao.GridAreaCode == gridAreaOwnershipAssignedDto4.GridAreaCode
                            && gao.ValidFrom == gridAreaOwnershipAssignedDto4.ValidFrom.ToDateTimeUtc()
                            && gao.GridAreaOwnerActorNumber == gridAreaOwnershipAssignedDto4.GridAreaOwner.Value
                            && gao.SequenceNumber == gridAreaOwnershipAssignedDto4.SequenceNumber,
                        gao =>
                            gao.GridAreaCode == gridAreaOwnershipAssignedDto5.GridAreaCode
                            && gao.ValidFrom == gridAreaOwnershipAssignedDto5.ValidFrom.ToDateTimeUtc()
                            && gao.GridAreaOwnerActorNumber == gridAreaOwnershipAssignedDto5.GridAreaOwner.Value
                            && gao.SequenceNumber == gridAreaOwnershipAssignedDto5.SequenceNumber);
            }
        }

        public sealed class GetGridOwnerForGridAreaCodeAsync : GridAreaOwnershipTests
        {
            public GetGridOwnerForGridAreaCodeAsync(DatabaseFixture databaseFixture)
                : base(databaseFixture)
            {
                ((SystemDateTimeProviderStub)GetService<ISystemDateTimeProvider>()).SetNow(SampleData.StartOfPeriod);
            }

            [Fact]
            public async Task Requesting_grid_owner_with_an_empty_db_fails()
            {
                var act = async () => await _masterDataClient.GetGridOwnerForGridAreaCodeAsync(
                    SampleData.GridAreaCode,
                    CancellationToken.None);

                await act.Should()
                    .ThrowAsync<InvalidOperationException>(
                        "all grid areas are defined beforehand and all grid areas are owned. Unknown and/or unowned grid areas are thus by definition an error");
            }

            [Fact]
            public async Task Requesting_grid_owner_for_an_unknown_area_fails()
            {
                var gridAreaOwnershipAssignedDto = CreateGridAreaOwnershipAssignedDto();

                await _masterDataClient.UpdateGridAreaOwnershipAsync(
                    gridAreaOwnershipAssignedDto,
                    CancellationToken.None);

                await _unitOfWork.CommitTransactionAsync();

                var act = async () => await _masterDataClient.GetGridOwnerForGridAreaCodeAsync(
                    "806",
                    CancellationToken.None);

                await act.Should()
                    .ThrowAsync<InvalidOperationException>(
                        "all grid areas are defined beforehand and all grid areas are owned. Unknown and/or unowned grid areas are thus by definition an error");
            }

            [Fact]
            public async Task Requesting_grid_owner_for_known_area_with_single_entry_gives_current_owner()
            {
                var gridAreaOwnershipAssignedDto = CreateGridAreaOwnershipAssignedDto();

                await _masterDataClient.UpdateGridAreaOwnershipAsync(
                    gridAreaOwnershipAssignedDto,
                    CancellationToken.None);

                await _unitOfWork.CommitTransactionAsync();

                var result = await _masterDataClient.GetGridOwnerForGridAreaCodeAsync(
                    SampleData.GridAreaCode,
                    CancellationToken.None);

                result.Should().Be(SampleData.GridOperatorNumber);
            }

            [Fact]
            public async Task Requesting_grid_owner_for_area_with_history_gives_current_owner()
            {
                var gridAreaOwnershipAssignedDto1 = new GridAreaOwnershipAssignedDto(
                    SampleData.GridAreaCode,
                    SampleData.StartOfPeriod.Minus(Duration.FromDays(42)),
                    SampleData.GridOperatorNumber,
                    41);

                var gridAreaOwnershipAssignedDto2 = new GridAreaOwnershipAssignedDto(
                    SampleData.GridAreaCode,
                    SampleData.StartOfPeriod,
                    SampleData.BalanceResponsibleNumber,
                    42);

                await _masterDataClient.UpdateGridAreaOwnershipAsync(
                    gridAreaOwnershipAssignedDto1,
                    CancellationToken.None);

                await _masterDataClient.UpdateGridAreaOwnershipAsync(
                    gridAreaOwnershipAssignedDto2,
                    CancellationToken.None);

                await _unitOfWork.CommitTransactionAsync();

                var result = await _masterDataClient.GetGridOwnerForGridAreaCodeAsync(
                    SampleData.GridAreaCode,
                    CancellationToken.None);

                result.Should().Be(SampleData.BalanceResponsibleNumber);
            }

            [Fact]
            public async Task Requesting_grid_owner_for_area_with_future_new_owner_gives_current_owner()
            {
                var gridAreaOwnershipAssignedDto1 = new GridAreaOwnershipAssignedDto(
                    SampleData.GridAreaCode,
                    SampleData.StartOfPeriod.Minus(Duration.FromDays(42)),
                    SampleData.GridOperatorNumber,
                    41);

                var gridAreaOwnershipAssignedDto2 = new GridAreaOwnershipAssignedDto(
                    SampleData.GridAreaCode,
                    SampleData.StartOfPeriod.Plus(Duration.FromDays(42)),
                    SampleData.BalanceResponsibleNumber,
                    42);

                await _masterDataClient.UpdateGridAreaOwnershipAsync(
                    gridAreaOwnershipAssignedDto1,
                    CancellationToken.None);

                await _masterDataClient.UpdateGridAreaOwnershipAsync(
                    gridAreaOwnershipAssignedDto2,
                    CancellationToken.None);

                await _unitOfWork.CommitTransactionAsync();

                var result = await _masterDataClient.GetGridOwnerForGridAreaCodeAsync(
                    SampleData.GridAreaCode,
                    CancellationToken.None);

                result.Should().Be(SampleData.GridOperatorNumber);
            }
        }

        private sealed record GridAreaOwner(
            string GridAreaCode,
            DateTime ValidFrom,
            string GridAreaOwnerActorNumber,
            int SequenceNumber);
    }

    public abstract class ActorCertificatesTests : MasterDataClientTests
    {
        private static readonly CertificateThumbprintDto _certificateThumbprintDto = new("ThisIsThumbprint");

        private ActorCertificatesTests(DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
        }

        private static ActorCertificateCredentialsAssignedDto CreateActorCertificateCredentialsAssignedDto()
        {
            return new ActorCertificateCredentialsAssignedDto(
                SampleData.GridOperatorNumber,
                MarketRole.GridOperator,
                _certificateThumbprintDto,
                SampleData.StartOfPeriod,
                42);
        }

        private async Task<IEnumerable<ActorCertificate>> GetActorCertificates()
        {
            using var connection = await _connectionFactory.GetConnectionAndOpenAsync(CancellationToken.None);
            var sql = $"SELECT ActorNumber, ActorRole, Thumbprint, ValidFrom, SequenceNumber " +
                      $"FROM [dbo].[ActorCertificate]";

            return await connection.QueryAsync<ActorCertificate>(sql);
        }

        public sealed class CreateOrUpdateActorCertificateAsync : ActorCertificatesTests
        {
            public CreateOrUpdateActorCertificateAsync(DatabaseFixture databaseFixture)
                : base(databaseFixture)
            {
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

                actorCertificates.Should().ContainSingle();
                var actorCertificate = actorCertificates.First();
                actorCertificate.ActorNumber.Should().Be(SampleData.GridOperatorNumber.Value);
                actorCertificate.ActorRole.Should().Be(MarketRole.GridOperator.Code);
                actorCertificate.Thumbprint.Should().Be(_certificateThumbprintDto.Thumbprint);
                actorCertificate.ValidFrom.Should().Be(SampleData.StartOfPeriod.ToDateTimeUtc());
                actorCertificate.SequenceNumber.Should().Be(42);
            }

            [Fact]
            public async Task Actor_certificate_is_created_and_updated_in_same_commit_fails()
            {
                var actorCertificateCredentialsAssignedDto = CreateActorCertificateCredentialsAssignedDto();

                await _masterDataClient.CreateOrUpdateActorCertificateAsync(
                    actorCertificateCredentialsAssignedDto,
                    CancellationToken.None);

                var updatedActorCertificateCredentialsAssignedDto = actorCertificateCredentialsAssignedDto with
                {
                    SequenceNumber = actorCertificateCredentialsAssignedDto.SequenceNumber + 1,
                    ThumbprintDto = new CertificateThumbprintDto(_certificateThumbprintDto.Thumbprint + "1"),
                };

                await _masterDataClient.CreateOrUpdateActorCertificateAsync(
                    updatedActorCertificateCredentialsAssignedDto,
                    CancellationToken.None);

                var act = async () => await _unitOfWork.CommitTransactionAsync();
                var exception = await act.Should().ThrowAsync<DbUpdateException>();
                exception
                    .WithMessage(
                        "An error occurred while saving the entity changes. See the inner exception for details.")
                    .WithInnerException<SqlException>()
                    .WithMessage(
                        "Cannot insert duplicate key row in object 'dbo.ActorCertificate' with unique index 'UX_ActorCertificate_ActorNumber_ActorRole'. The duplicate key value is (8200000007739, DDM).\nThe statement has been terminated.");
            }

            [Fact]
            public async Task Actor_certificate_created_multiple_times_within_single_commit_fails_with_db_error()
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

                var act = async () => await _unitOfWork.CommitTransactionAsync();
                var exception = await act.Should().ThrowAsync<DbUpdateException>();
                exception
                    .WithMessage(
                        "An error occurred while saving the entity changes. See the inner exception for details.")
                    .WithInnerException<SqlException>()
                    .WithMessage(
                        "Cannot insert duplicate key row in object 'dbo.ActorCertificate' with unique index 'UX_ActorCertificate_ActorNumber_ActorRole'. The duplicate key value is (8200000007739, DDM).\nThe statement has been terminated.");
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

                actorCertificates.Should().ContainSingle();
                var actorCertificate = actorCertificates.First();
                actorCertificate.ActorNumber.Should().Be(SampleData.GridOperatorNumber.Value);
                actorCertificate.ActorRole.Should().Be(MarketRole.GridOperator.Code);
                actorCertificate.Thumbprint.Should().Be(_certificateThumbprintDto.Thumbprint);
                actorCertificate.ValidFrom.Should().Be(SampleData.StartOfPeriod.ToDateTimeUtc());
                actorCertificate.SequenceNumber.Should().Be(42);
            }

            [Fact]
            public async Task Actor_certificate_can_be_updated_with_new_sequence_number()
            {
                var actorCertificateCredentialsAssignedDto = CreateActorCertificateCredentialsAssignedDto();

                await _masterDataClient.CreateOrUpdateActorCertificateAsync(
                    actorCertificateCredentialsAssignedDto,
                    CancellationToken.None);

                await _unitOfWork.CommitTransactionAsync();

                var updatedActorCertificateCredentialsAssignedDto = actorCertificateCredentialsAssignedDto with
                {
                    SequenceNumber = actorCertificateCredentialsAssignedDto.SequenceNumber + 1,
                    ThumbprintDto = new CertificateThumbprintDto(_certificateThumbprintDto.Thumbprint + "1"),
                };

                await _masterDataClient.CreateOrUpdateActorCertificateAsync(
                    updatedActorCertificateCredentialsAssignedDto,
                    CancellationToken.None);

                await _unitOfWork.CommitTransactionAsync();

                var actorCertificates = (await GetActorCertificates()).ToList();

                actorCertificates.Should().ContainSingle();
                var actorCertificate = actorCertificates.First();
                actorCertificate.ActorNumber.Should().Be(SampleData.GridOperatorNumber.Value);
                actorCertificate.ActorRole.Should().Be(MarketRole.GridOperator.Code);
                actorCertificate.Thumbprint.Should().Be(_certificateThumbprintDto.Thumbprint + "1");
                actorCertificate.ValidFrom.Should().Be(SampleData.StartOfPeriod.ToDateTimeUtc());
                actorCertificate.SequenceNumber.Should().Be(43);
            }

            [Fact]
            public async Task Actor_certificate_cannot_be_updated_with_same_or_lower_sequence_number()
            {
                var actorCertificateCredentialsAssignedDto = CreateActorCertificateCredentialsAssignedDto();

                await _masterDataClient.CreateOrUpdateActorCertificateAsync(
                    actorCertificateCredentialsAssignedDto,
                    CancellationToken.None);

                await _unitOfWork.CommitTransactionAsync();

                var updatedActorCertificateCredentialsAssignedDto1 = actorCertificateCredentialsAssignedDto with
                {
                    ThumbprintDto = new CertificateThumbprintDto(_certificateThumbprintDto.Thumbprint + "_same"),
                };

                await _masterDataClient.CreateOrUpdateActorCertificateAsync(
                    updatedActorCertificateCredentialsAssignedDto1,
                    CancellationToken.None);

                await _unitOfWork.CommitTransactionAsync();

                var updatedActorCertificateCredentialsAssignedDto2 = actorCertificateCredentialsAssignedDto with
                {
                    SequenceNumber = actorCertificateCredentialsAssignedDto.SequenceNumber - 1,
                    ThumbprintDto = new CertificateThumbprintDto(_certificateThumbprintDto.Thumbprint + "-1"),
                };

                await _masterDataClient.CreateOrUpdateActorCertificateAsync(
                    updatedActorCertificateCredentialsAssignedDto2,
                    CancellationToken.None);

                await _unitOfWork.CommitTransactionAsync();

                var actorCertificates = (await GetActorCertificates()).ToList();

                actorCertificates.Should().ContainSingle();
                var actorCertificate = actorCertificates.First();
                actorCertificate.ActorNumber.Should().Be(SampleData.GridOperatorNumber.Value);
                actorCertificate.ActorRole.Should().Be(MarketRole.GridOperator.Code);
                actorCertificate.Thumbprint.Should().Be(_certificateThumbprintDto.Thumbprint);
                actorCertificate.ValidFrom.Should().Be(SampleData.StartOfPeriod.ToDateTimeUtc());
                actorCertificate.SequenceNumber.Should().Be(42);
            }
        }

        public sealed class GetActorNumberAndRoleFromThumbprintAsync : ActorCertificatesTests
        {
            public GetActorNumberAndRoleFromThumbprintAsync(DatabaseFixture databaseFixture)
                : base(databaseFixture)
            {
            }

            [Fact]
            public async Task Requesting_actor_information_from_empty_db_gives_null_result()
            {
                var result =
                    await _masterDataClient.GetActorNumberAndRoleFromThumbprintAsync(_certificateThumbprintDto);

                result.Should().BeNull();
            }

            [Fact]
            public async Task Requesting_actor_information_for_unknown_thumbprint_gives_null()
            {
                var actorCertificateCredentialsAssignedDto = CreateActorCertificateCredentialsAssignedDto();

                await _masterDataClient.CreateOrUpdateActorCertificateAsync(
                    actorCertificateCredentialsAssignedDto,
                    CancellationToken.None);

                await _unitOfWork.CommitTransactionAsync();

                var result = await _masterDataClient.GetActorNumberAndRoleFromThumbprintAsync(
                    new CertificateThumbprintDto("unknown"));

                result.Should().BeNull();
            }

            [Fact]
            public async Task Requesting_actor_information_for_known_thumbprint_returns_associated_actor_information()
            {
                var actorCertificateCredentialsAssignedDto = CreateActorCertificateCredentialsAssignedDto();

                await _masterDataClient.CreateOrUpdateActorCertificateAsync(
                    actorCertificateCredentialsAssignedDto,
                    CancellationToken.None);

                await _unitOfWork.CommitTransactionAsync();

                var result = await _masterDataClient.GetActorNumberAndRoleFromThumbprintAsync(
                    _certificateThumbprintDto);

                result.Should().NotBeNull();
                result!.ActorNumber.Should().NotBeNull();
                result.MarketRole.Should().NotBeNull();
                result.ActorNumber.Should().Be(SampleData.GridOperatorNumber);
                result.MarketRole.Should().Be(MarketRole.GridOperator);
            }
        }

        public sealed class DeleteActorCertificateAsync : ActorCertificatesTests
        {
            public DeleteActorCertificateAsync(DatabaseFixture databaseFixture)
                : base(databaseFixture)
            {
            }

            [Fact]
            public async Task Delete_actor_certificate_with_an_empty_db_does_nothing()
            {
                var actorCertificateCredentialsRemovedDto = new ActorCertificateCredentialsRemovedDto(
                    SampleData.GridOperatorNumber,
                    _certificateThumbprintDto);

                await _masterDataClient.DeleteActorCertificateAsync(
                    actorCertificateCredentialsRemovedDto,
                    CancellationToken.None);

                await _unitOfWork.CommitTransactionAsync();

                var actorCertificates = (await GetActorCertificates()).ToList();

                actorCertificates.Should().BeEmpty();
            }

            [Fact]
            public async Task Deleting_certificate_that_does_not_exist_does_nothing()
            {
                var actorCertificateCredentialsAssignedDto = CreateActorCertificateCredentialsAssignedDto();

                await _masterDataClient.CreateOrUpdateActorCertificateAsync(
                    actorCertificateCredentialsAssignedDto,
                    CancellationToken.None);

                await _unitOfWork.CommitTransactionAsync();

                var actorCertificateCredentialsRemovedDto = new ActorCertificateCredentialsRemovedDto(
                    SampleData.GridOperatorNumber,
                    new CertificateThumbprintDto("unknown"));

                await _masterDataClient.DeleteActorCertificateAsync(
                    actorCertificateCredentialsRemovedDto,
                    CancellationToken.None);

                await _unitOfWork.CommitTransactionAsync();

                var actorCertificates = (await GetActorCertificates()).ToList();

                actorCertificates.Should().ContainSingle();
            }

            [Fact]
            public async Task Deleting_existing_actor_certificate_removes_it_from_db()
            {
                var actorCertificateCredentialsAssignedDto = CreateActorCertificateCredentialsAssignedDto();

                await _masterDataClient.CreateOrUpdateActorCertificateAsync(
                    actorCertificateCredentialsAssignedDto,
                    CancellationToken.None);

                await _unitOfWork.CommitTransactionAsync();

                (await GetActorCertificates()).ToList().Should().NotBeNullOrEmpty();

                var actorCertificateCredentialsRemovedDto = new ActorCertificateCredentialsRemovedDto(
                    SampleData.GridOperatorNumber,
                    _certificateThumbprintDto);

                await _masterDataClient.DeleteActorCertificateAsync(
                    actorCertificateCredentialsRemovedDto,
                    CancellationToken.None);

                await _unitOfWork.CommitTransactionAsync();

                var actorCertificates = (await GetActorCertificates()).ToList();

                actorCertificates.Should().BeEmpty();
            }
        }

        private sealed record ActorCertificate(
            string ActorNumber,
            string ActorRole,
            string Thumbprint,
            DateTime ValidFrom,
            int SequenceNumber);
    }

    private static class SampleData
    {
        internal static string SomeActorNumber => "5148796574821";

        internal static string ExternalId => Guid.Parse("9222905B-8B02-4D8B-A2C1-3BD51B1AD8D9").ToString();

        internal static ActorNumber GridOperatorNumber => ActorNumber.Create("8200000007739");

        internal static string GridAreaCode => "805";

        internal static Instant StartOfPeriod => EffectiveDateFactory.InstantAsOfToday();

        internal static Instant EndOfPeriod => EffectiveDateFactory.OffsetDaysFromToday(1);

        internal static ActorNumber EnergySupplierNumber => ActorNumber.Create("8200000007740");

        internal static ActorNumber EnergySupplierNumber2 => ActorNumber.Create("8200000007742");

        internal static ActorNumber BalanceResponsibleNumber => ActorNumber.Create("8200000007743");
    }
}
