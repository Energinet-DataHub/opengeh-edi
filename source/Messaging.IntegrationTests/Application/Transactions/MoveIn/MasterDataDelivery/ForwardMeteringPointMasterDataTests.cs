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
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Application.MasterData;
using Messaging.Application.OutgoingMessages.Common;
using Messaging.Application.Transactions.MoveIn.MasterDataDelivery;
using Messaging.Domain.MasterData.Dictionaries;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.OutgoingMessages.AccountingPointCharacteristics;
using Messaging.Domain.OutgoingMessages.AccountingPointCharacteristics.MarketEvaluationPointDetails;
using Messaging.Infrastructure.Configuration.DataAccess;
using Messaging.IntegrationTests.Fixtures;
using NodaTime.Extensions;
using Xunit;
using Address = Messaging.Application.MasterData.Address;
using Series = Messaging.Application.MasterData.Series;

namespace Messaging.IntegrationTests.Application.Transactions.MoveIn.MasterDataDelivery;

public class ForwardMeteringPointMasterDataTests : TestBase, IAsyncLifetime
{
    public ForwardMeteringPointMasterDataTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
    }

    public Task InitializeAsync()
    {
        return Scenario.Details(
                SampleData.TransactionId,
                SampleData.MarketEvaluationPointId,
                SampleData.SupplyStart,
                SampleData.CurrentEnergySupplierNumber,
                SampleData.NewEnergySupplierNumber,
                SampleData.ConsumerId,
                SampleData.ConsumerIdType,
                SampleData.ConsumerName,
                SampleData.OriginalMessageId,
                GetService<IMediator>(),
                GetService<B2BContext>())
            .IsEffective()
            .WithGridOperatorForMeteringPoint(
                SampleData.IdOfGridOperatorForMeteringPoint,
                SampleData.NumberOfGridOperatorForMeteringPoint)
            .BuildAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Metering_point_master_data_is_forwarded_to_the_new_energy_supplier()
    {
        var forwardMeteringPointMasterData = new ForwardMeteringPointMasterData(SampleData.TransactionId, CreateMasterDataContent());
        await InvokeCommandAsync(forwardMeteringPointMasterData).ConfigureAwait(false);

        var assertTransaction = await AssertTransaction.TransactionAsync(SampleData.ActorProvidedId, GetService<IDatabaseConnectionFactory>()).ConfigureAwait(false);
        assertTransaction.MeteringPointMasterDataWasSent();
    }

    [Fact]
    public async Task Correct_metering_point_master_data_message_is_added_to_outgoing_message_store()
    {
        var masterData = CreateMasterDataContent();

        var forwardMeteringPointMasterData = new ForwardMeteringPointMasterData(SampleData.TransactionId, masterData);
        await InvokeCommandAsync(forwardMeteringPointMasterData).ConfigureAwait(false);

        var marketActivityRecord = await GetMarketActivityRecordAsync(MessageType.AccountingPointCharacteristics).ConfigureAwait(false);
        AssertMarketEvaluationPoint(masterData, marketActivityRecord.MarketEvaluationPt);
    }

    [Fact]
    public async Task Correct_metering_point_master_data_message_is_added_to_outgoing_message_store_when_masterdata_contains_null_values()
    {
        var masterData = CreateMasterDataContentWithNullValues();

        var forwardMeteringPointMasterData = new ForwardMeteringPointMasterData(SampleData.TransactionId, masterData);
        await InvokeCommandAsync(forwardMeteringPointMasterData).ConfigureAwait(false);

        var marketActivityRecord = await GetMarketActivityRecordAsync(MessageType.AccountingPointCharacteristics).ConfigureAwait(false);
        AssertMarketEvaluationPoint(masterData, marketActivityRecord.MarketEvaluationPt);
    }

    private static void AssertMarketEvaluationPoint(MasterDataContent masterDataContent, MarketEvaluationPoint marketEvaluationPoint)
    {
        Assert.Equal(SampleData.MarketEvaluationPointId, marketEvaluationPoint.MRID.Id);
        Assert.Null(marketEvaluationPoint.MeteringPointResponsible);
        Assert.Equal(MasterDataTranslation.GetTranslationFrom(masterDataContent.Type), marketEvaluationPoint.Type);
        Assert.Equal(MasterDataTranslation.GetTranslationFrom(masterDataContent.SettlementMethod), marketEvaluationPoint.SettlementMethod);
        Assert.Equal(MasterDataTranslation.GetTranslationFrom(masterDataContent.MeteringMethod), marketEvaluationPoint.MeteringMethod);
        Assert.Equal(MasterDataTranslation.GetTranslationFrom(masterDataContent.ConnectionState), marketEvaluationPoint.ConnectionState);
        Assert.Equal(MasterDataTranslation.GetTranslationFrom(masterDataContent.ReadingPeriodicity), marketEvaluationPoint.ReadCycle);
        Assert.Equal(MasterDataTranslation.GetTranslationFrom(masterDataContent.NetSettlementGroup), marketEvaluationPoint.NetSettlementGroup);
        Assert.Equal(MasterDataTranslation.TranslateToNextReadingDate(masterDataContent.ScheduledMeterReadingDate).Date, marketEvaluationPoint.NextReadingDate.Date);
        Assert.Equal(masterDataContent.GridAreaDetails.Code, marketEvaluationPoint.MeteringGridAreaId.Id);
        Assert.Null(marketEvaluationPoint.OutMeteringGridAreaId);
        Assert.Null(marketEvaluationPoint.InMeteringGridAreaId);
        Assert.Equal(masterDataContent.PowerPlantGsrnNumber, marketEvaluationPoint.LinkedMarketEvaluationPointId.Id);
        Assert.Equal(
            masterDataContent.Capacity.ToString(CultureInfo.InvariantCulture),
            marketEvaluationPoint.PhysicalConnectionCapacity.Value);
        Assert.Equal(MasterDataTranslation.GetTranslationFrom(masterDataContent.ConnectionType), marketEvaluationPoint.ConnectionType);
        Assert.Equal(MasterDataTranslation.GetTranslationFrom(masterDataContent.DisconnectionType), marketEvaluationPoint.DisconnectionMethod);
        Assert.Equal(MasterDataTranslation.GetTranslationFrom(masterDataContent.AssetType), marketEvaluationPoint.PsrType);
        Assert.Equal(
            masterDataContent.ProductionObligation.ToString(CultureInfo.InvariantCulture),
            marketEvaluationPoint.ProductionObligation);
        Assert.Equal(
            masterDataContent.MaximumPower.ToString(CultureInfo.InvariantCulture),
            marketEvaluationPoint.ContractedConnectionCapacity.Value);
        Assert.Equal(
            masterDataContent.MaximumCurrent.ToString(CultureInfo.InvariantCulture),
            marketEvaluationPoint.RatedCurrent.Value);
        Assert.Equal(masterDataContent.MeterNumber, marketEvaluationPoint.MeterId);
        Assert.Equal(MasterDataTranslation.GetTranslationFrom(masterDataContent.Series.Product), marketEvaluationPoint.Series.Product);
        Assert.Equal(MasterDataTranslation.GetTranslationFrom(masterDataContent.Series.UnitType), marketEvaluationPoint.Series.QuantityMeasureUnit);
        Assert.Equal(masterDataContent.EffectiveDate.ToUniversalTime().ToInstant(), marketEvaluationPoint.SupplyStart);
        Assert.Equal(masterDataContent.Address.LocationDescription, marketEvaluationPoint.Description);
        Assert.Equal(masterDataContent.Address.GeoInfoReference.ToString(), marketEvaluationPoint.GeoInfoReference);
        Assert.Equal(masterDataContent.Address.IsActualAddress.ToString(), marketEvaluationPoint.IsActualAddress);
        Assert.Null(marketEvaluationPoint.ParentMarketEvaluationPoint);
        Assert.Equal(masterDataContent.Address.StreetCode, marketEvaluationPoint.MainAddress.Street.Code);
        Assert.Equal(masterDataContent.Address.StreetName, marketEvaluationPoint.MainAddress.Street.Name);
        Assert.Equal(masterDataContent.Address.BuildingNumber, marketEvaluationPoint.MainAddress.Street.Number);
        Assert.Equal(masterDataContent.Address.Floor, marketEvaluationPoint.MainAddress.Street.FloorIdentification);
        Assert.Equal(masterDataContent.Address.Room, marketEvaluationPoint.MainAddress.Street.SuiteNumber);
        Assert.Equal(
            masterDataContent.Address.MunicipalityCode.ToString(CultureInfo.InvariantCulture),
            marketEvaluationPoint.MainAddress.Town.Code);
        Assert.Equal(masterDataContent.Address.City, marketEvaluationPoint.MainAddress.Town.Name);
        Assert.Equal(masterDataContent.Address.CitySubDivision, marketEvaluationPoint.MainAddress.Town.Section);
        Assert.Equal(masterDataContent.Address.CountryCode, marketEvaluationPoint.MainAddress.Town.Country);
    }

    private static MasterDataContent CreateMasterDataContent()
    {
        return new MasterDataContent(
            SampleData.MeteringPointNumber,
            new Address(
                MasterDataSampleData.StreetName,
                MasterDataSampleData.StreetCode,
                MasterDataSampleData.PostCode,
                MasterDataSampleData.CityName,
                MasterDataSampleData.CountryCode,
                MasterDataSampleData.CitySubdivision,
                MasterDataSampleData.Floor,
                MasterDataSampleData.Room,
                MasterDataSampleData.BuildingNumber,
                MasterDataSampleData.MunicipalityCode,
                MasterDataSampleData.IsActualAddress,
                Guid.Parse(MasterDataSampleData.GeoInfoReference),
                MasterDataSampleData.LocationDescription),
            new Series(
                MasterDataSampleData.ProductType,
                MasterDataSampleData.UnitType),
            new GridAreaDetails(
                "some string code",
                "some string to code",
                "some string from code"),
            string.Empty,
            string.Empty,
            string.Empty,
            MasterDataSampleData.TypeName,
            MasterDataSampleData.MaximumCurrent,
            MasterDataSampleData.MaximumPower,
            MasterDataSampleData.PowerPlant,
            DateTime.Parse(MasterDataSampleData.EffectiveDate, CultureInfo.InvariantCulture),
            MasterDataSampleData.MeterNumber,
            double.Parse(MasterDataSampleData.Capacity, CultureInfo.InvariantCulture),
            MasterDataSampleData.AssetType,
            MasterDataSampleData.SettlementMethod,
            MasterDataSampleData.ScheduledMeterReadingDate,
            false,
            MasterDataSampleData.NetSettlementGroup,
            MasterDataSampleData.DisconnectionType,
            MasterDataSampleData.ConnectionType,
            MasterDataSampleData.ParentRelatedMeteringPoint,
            null);
    }

    private static MasterDataContent CreateMasterDataContentWithNullValues()
    {
        return new MasterDataContent(
            SampleData.MeteringPointNumber,
            new Address(
                MasterDataSampleData.StreetName,
                MasterDataSampleData.StreetCode,
                MasterDataSampleData.PostCode,
                MasterDataSampleData.CityName,
                MasterDataSampleData.CountryCode,
                MasterDataSampleData.CitySubdivision,
                MasterDataSampleData.Floor,
                MasterDataSampleData.Room,
                MasterDataSampleData.BuildingNumber,
                MasterDataSampleData.MunicipalityCode,
                MasterDataSampleData.IsActualAddress,
                Guid.Parse(MasterDataSampleData.GeoInfoReference),
                MasterDataSampleData.LocationDescription),
            new Series(
                MasterDataSampleData.ProductType,
                MasterDataSampleData.UnitType),
            new GridAreaDetails(
                "some string code",
                "some string to code",
                "some string from code"),
            string.Empty,
            string.Empty,
            string.Empty,
            MasterDataSampleData.TypeName,
            MasterDataSampleData.MaximumCurrent,
            MasterDataSampleData.MaximumPower,
            MasterDataSampleData.PowerPlant,
            DateTime.Parse(MasterDataSampleData.EffectiveDate, CultureInfo.InvariantCulture),
            MasterDataSampleData.MeterNumber,
            double.Parse(MasterDataSampleData.Capacity, CultureInfo.InvariantCulture),
            MasterDataSampleData.AssetType,
            null!,
            MasterDataSampleData.ScheduledMeterReadingDate,
            false,
            MasterDataSampleData.NetSettlementGroup,
            MasterDataSampleData.DisconnectionType,
            null!,
            MasterDataSampleData.ParentRelatedMeteringPoint,
            null);
    }

    private Task<MarketActivityRecord> GetMarketActivityRecordAsync(MessageType messageType)
    {
        var parser = GetService<IMessageRecordParser>();
        var message = GetService<B2BContext>().OutgoingMessages.First(m => m.MessageType == messageType);
        var marketActivityRecord =
            parser.From<MarketActivityRecord>(
                message!.MessageRecord);
        return Task.FromResult(marketActivityRecord);
    }
}
