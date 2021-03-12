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
using System.Threading.Tasks;
using Energinet.DataHub.Ingestion.Application.ChangeOfCharges;
using Energinet.DataHub.Ingestion.Application.ChangeOfCharges.ValidationRules;
using Energinet.DataHub.Ingestion.Domain.ChangeOfCharges;
using Energinet.DataHub.Ingestion.Infrastructure.ChangeOfCharges.Context;
using Energinet.DataHub.Ingestion.Infrastructure.ChangeOfCharges.Context.Model;
using Energinet.DataHub.Ingestion.Infrastructure.ChangeOfCharges.Mapping;
using Energinet.DataHub.Ingestion.Infrastructure.ChangeOfCharges.Repositories;
using GreenEnergyHub.TestHelpers;
using GreenEnergyHub.TestHelpers.Traits;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Xunit;
using ChargeType = Energinet.DataHub.Ingestion.Infrastructure.ChangeOfCharges.Context.Model.ChargeType;

namespace Energinet.DataHub.Ingestion.Tests.Infrastructure
{
    /// <summary>
    /// Tests <see cref="ChargeRepository"/> using an SQLite in-memory database.
    /// </summary>
    [Trait(TraitNames.Category, TraitValues.UnitTest)]
    public class ChargeRepositoryTest
    {
        private const string KnownChargeType = "knownChargeType";
        private const string KnownResolutionType = "knownResolutionType";
        private const string KnownVatPayer = "knownVatPayer";
        private const string KnownChargeOwner = "knownChargeOwner";

        private readonly DbContextOptions<ChargesDatabaseContext> _dbContextOptions = new DbContextOptionsBuilder<ChargesDatabaseContext>()
            .UseSqlite("Filename=Test.db")
            .Options;

        [Theory]
        [InlineAutoDomainData("unknown", "NotUsed", "NotUsed", "NotUsed", "No charge type for unknown")]
        [InlineAutoDomainData(KnownChargeType, "unknown", "NotUsed", "NotUsed", "No resolution type for unknown")]
        [InlineAutoDomainData(KnownChargeType, KnownResolutionType, "unknown", "NotUsed", "No VAT payer type for unknown")]
        [InlineAutoDomainData(KnownChargeType, KnownResolutionType, KnownVatPayer, "unknown", "No market participant for unknown")]
        public async Task StoreChargeAsync_WhenValueNotFoundInDbContext_ThenFailureStatusReturnedAsync(string chargeType, string resolutionType, string vatPayerType, string chargeOwner, string failureReason)
        {
            // Arrange
            var message = GetValidChangeOfChargesMessage();
            message.Type = chargeType;
            message.ChargeTypeOwnerMRid = chargeOwner;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            message.MktActivityRecord.ChargeType.VATPayer = vatPayerType;
            message.Period.Resolution = resolutionType;
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            SeedDatabaseContext();
            var sut = new ChargeRepository(new ChargesDatabaseContext(_dbContextOptions));

            // Act
            var result = await sut.StoreChargeAsync(message).ConfigureAwait(false);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(failureReason, result.Reason);
        }

        #region Argument validation
        [Theory]
        [InlineAutoDomainData(null, "NotUsed", "NotUsed", "NotUsed", "Fails as Type is invalid")]
        [InlineAutoDomainData(" ", "NotUsed", "NotUsed", "NotUsed", "Fails as Type is invalid")]
        [InlineAutoDomainData(KnownChargeType, null, "NotUsed", "NotUsed", "Fails as Resolution is invalid")]
        [InlineAutoDomainData(KnownChargeType, " ", "NotUsed", "NotUsed", "Fails as Resolution is invalid")]
        [InlineAutoDomainData(KnownChargeType, KnownResolutionType, null, "NotUsed", "Fails as VATPayer is invalid")]
        [InlineAutoDomainData(KnownChargeType, KnownResolutionType, " ", "NotUsed", "Fails as VATPayer is invalid")]
        [InlineAutoDomainData(KnownChargeType, KnownResolutionType, KnownVatPayer, null, "Fails as ChargeTypeOwnerMRid is invalid")]
        [InlineAutoDomainData(KnownChargeType, KnownResolutionType, KnownVatPayer, " ", "Fails as ChargeTypeOwnerMRid is invalid")]
        public async Task StoreChargeAsync_WhenValuesInMessageUsedForDbContextLookupsAreInvalid_ThenExceptionThrownAsync(string chargeType, string resolutionType, string vatPayerType, string chargeOwner, string exceptionMessage)
        {
            // Arrange
            var message = GetValidChangeOfChargesMessage();
            message.Type = chargeType;
            message.ChargeTypeOwnerMRid = chargeOwner;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            message.MktActivityRecord.ChargeType.VATPayer = vatPayerType;
            message.Period.Resolution = resolutionType;
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            SeedDatabaseContext();
            var sut = new ChargeRepository(new ChargesDatabaseContext(_dbContextOptions));

            // Act
            var exception = await Record.ExceptionAsync(async () => await sut.StoreChargeAsync(message).ConfigureAwait(false));

            // Assert
            Assert.IsType<ArgumentException>(exception);
            Assert.Contains(exceptionMessage, exception.Message);
        }

        [Theory]
        [InlineAutoDomainData(null, "Valid", "Valid", "Valid", "Valid", "ChargeTypeMRid")]
        [InlineAutoDomainData(" ", "Valid", "Valid", "Valid", "Valid", "ChargeTypeMRid")]
        [InlineAutoDomainData("Valid", null, "Valid", "Valid", "Valid", "CorrelationId")]
        [InlineAutoDomainData("Valid", " ", "Valid", "Valid", "Valid", "CorrelationId")]
        [InlineAutoDomainData("Valid", "Valid", null, "Valid", "Valid", "LastUpdatedBy")]
        [InlineAutoDomainData("Valid", "Valid", " ", "Valid", "Valid", "LastUpdatedBy")]
        [InlineAutoDomainData("Valid", "Valid", "Valid", null, "Valid", "Name")]
        [InlineAutoDomainData("Valid", "Valid", "Valid", " ", "Valid", "Name")]
        [InlineAutoDomainData("Valid", "Valid", "Valid", "Valid", null, "Description")]
        [InlineAutoDomainData("Valid", "Valid", "Valid", "Valid", " ", "Description")]
        public async Task StoreChargeAsync_WhenValuesInMessageAreInvalid_ThenExceptionThrownAsync(string chargeTypeMRid, string correlationId, string lastUpdatedBy, string shortDescription, string longDescription, string argumentThatFails)
        {
            // Arrange
            var message = GetValidChangeOfChargesMessage();
            message.ChargeTypeMRid = chargeTypeMRid;
            message.CorrelationId = correlationId;
            message.LastUpdatedBy = lastUpdatedBy;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            message.MktActivityRecord.ChargeType.Name = shortDescription;
            message.MktActivityRecord.ChargeType.Description = longDescription;
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            SeedDatabaseContext();
            var sut = new ChargeRepository(new ChargesDatabaseContext(_dbContextOptions));

            // Act
            var exception = await Record.ExceptionAsync(async () => await sut.StoreChargeAsync(message).ConfigureAwait(false));

            // Assert
            Assert.IsType<ArgumentException>(exception);
            Assert.Contains($"{argumentThatFails} must have value", exception.Message);
        }

        [Fact]
        public async Task StoreChargeAsync_WhenPointsIsNull_ThenExceptionThrownAsync()
        {
            // Arrange
            var message = GetValidChangeOfChargesMessage();
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            message.Period.Points = null;
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            SeedDatabaseContext();
            var sut = new ChargeRepository(new ChargesDatabaseContext(_dbContextOptions));

            // Act
            var exception = await Record.ExceptionAsync(async () => await sut.StoreChargeAsync(message).ConfigureAwait(false));

            // Assert
            Assert.IsType<ArgumentException>(exception);
            Assert.Contains($"{nameof(message.Period.Points)} can't be null", exception.Message);
        }
        #endregion

        [Fact]
        public async Task StoreChargeAsync_WhenChargeIsSaved_ThenSuccessReturnedAsync()
        {
            // Arrange
            var message = GetValidChangeOfChargesMessage();

            SeedDatabaseContext();
            var sut = new ChargeRepository(new ChargesDatabaseContext(_dbContextOptions));

            // Act
            var result = await sut.StoreChargeAsync(message).ConfigureAwait(false);

            // Assert
            Assert.True(result.Success);
        }

        [Fact]
        public void MapChangeOfChargesMessageToCharge_WhenMessageWithProperties_ThenReturnsChargeWithPropertiesSet()
        {
            // Arrange
            var changeOfChargesMessage = GetValidChangeOfChargesMessage();
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            changeOfChargesMessage.MktActivityRecord.ValidityEndDate = Instant.MaxValue;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            var chargeType = new ChargeType { Code = changeOfChargesMessage.Type, Id = 1, Name = "Name" };
            var chargeTypeOwnerMRid = new Ingestion.Infrastructure.ChangeOfCharges.Context.Model.MarketParticipant { Id = 1, MRid = changeOfChargesMessage.ChargeTypeOwnerMRid };
            var resolutionType = new ResolutionType { Id = 1, Name = changeOfChargesMessage.Period?.Resolution };
            var vatPayerType = new VatPayerType { Id = 1, Name = changeOfChargesMessage.MktActivityRecord?.ChargeType?.VATPayer };

            // When
            var result = ChangeOfChargesMapper.MapChangeOfChargesMessageToCharge(changeOfChargesMessage, chargeType, chargeTypeOwnerMRid, resolutionType, vatPayerType);

            var properties = result.GetType().GetProperties();
            foreach (var property in properties)
            {
                Assert.NotNull(property.GetValue(result));
            }
        }

        private static ChangeOfChargesMessage GetValidChangeOfChargesMessage()
        {
            var message = new ChangeOfChargesMessage
            {
                ChargeTypeMRid = "chargeTypeMRid",
                CorrelationId = "correlationId",
                LastUpdatedBy = "lastUpdatedBy",
                Type = KnownChargeType,
                ChargeTypeOwnerMRid = KnownChargeOwner,
                MktActivityRecord = new MktActivityRecord
                {
                    ChargeType = new Ingestion.Domain.ChangeOfCharges.ChargeType
                        { VATPayer = KnownVatPayer, Name = "shortDescription", Description = "longDescription", },
                },
                Period = new ChargeTypePeriod
                {
                    Resolution = KnownResolutionType, Points = new List<Point>
                    {
                        new Point { Position = 1, PriceAmount = 1m, Time = SystemClock.Instance.GetCurrentInstant(), },
                    },
                },
            };
            return message;
        }

        private void SeedDatabaseContext()
        {
            using var context = new ChargesDatabaseContext(_dbContextOptions);
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            var chargeTypes = new List<ChargeType> { new ChargeType { Code = KnownChargeType, Id = 1, } };
            context.AddRange(chargeTypes);

            var resolutionTypes = new List<ResolutionType> { new ResolutionType { Name = KnownResolutionType, Id = 1, } };
            context.AddRange(resolutionTypes);

            var vatPayerTypes = new List<VatPayerType> { new VatPayerType { Name = KnownVatPayer, Id = 1, } };
            context.AddRange(vatPayerTypes);

            var chargeOwners = new List<Ingestion.Infrastructure.ChangeOfCharges.Context.Model.MarketParticipant>
            {
                new Ingestion.Infrastructure.ChangeOfCharges.Context.Model.MarketParticipant
                {
                    MRid = KnownChargeOwner,
                    Id = 1,
                },
            };
            context.AddRange(chargeOwners);

            var ruleConfigurations = new List<RuleConfiguration>
            {
                new RuleConfiguration(ValidationRuleNames.StartOfValidIntervalFromNowInDays, "31"),
                new RuleConfiguration(ValidationRuleNames.EndOfValidIntervalFromNowInDays, "1095"),
            };
            context.AddRange(ruleConfigurations);

            context.SaveChanges();
        }
    }
}
