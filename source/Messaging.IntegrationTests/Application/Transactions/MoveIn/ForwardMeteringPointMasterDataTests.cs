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
using Dapper;
using Messaging.Application.Common;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Application.MasterData;
using Messaging.Application.OutgoingMessages;
using Messaging.Application.OutgoingMessages.AccountingPointCharacteristics;
using Messaging.Application.Transactions.MoveIn;
using Messaging.Domain.OutgoingMessages;
using Messaging.IntegrationTests.Application.IncomingMessages;
using Messaging.IntegrationTests.Fixtures;
using NodaTime.Extensions;
using Processing.IntegrationTests.Application;
using Xunit;
using Address = Messaging.Application.MasterData.Address;
using Series = Messaging.Application.MasterData.Series;

namespace Messaging.IntegrationTests.Application.Transactions.MoveIn;

public class ForwardMeteringPointMasterDataTests : TestBase
{
    public ForwardMeteringPointMasterDataTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
    }

    [Fact]
    public async Task Metering_point_master_data_is_forwarded_to_the_new_energy_supplier()
    {
        await SetupAnAcceptedMoveInTransaction().ConfigureAwait(false);

        var forwardMeteringPointMasterData = new ForwardMeteringPointMasterData(SampleData.TransactionId, CreateMasterDataContent());
        await InvokeCommandAsync(forwardMeteringPointMasterData).ConfigureAwait(false);

        AssertTransaction.Transaction(SampleData.TransactionId, GetService<IDbConnectionFactory>())
            .HasForwardedMeteringPointMasterData(true);
    }

    [Fact]
    public async Task Correct_metering_point_master_data_message_is_added_to_outgoing_message_store()
    {
        await SetupAnAcceptedMoveInTransaction().ConfigureAwait(false);
        var masterData = CreateMasterDataContent();

        var forwardMeteringPointMasterData = new ForwardMeteringPointMasterData(SampleData.TransactionId, masterData);
        await InvokeCommandAsync(forwardMeteringPointMasterData).ConfigureAwait(false);

        var marketActivityRecord = await GetMarketActivityRecordAsync("AccountingPointCharacteristics").ConfigureAwait(false);
        AssertMasterData(masterData, marketActivityRecord.MarketEvaluationPt);
    }

    private static void AssertMasterData(MasterDataContent masterDataContent, MarketEvaluationPoint marketEvaluationPoint)
    {
        Assert.Equal(SampleData.MarketEvaluationPointId, marketEvaluationPoint.MRID.Id);
        Assert.Equal(masterDataContent.MeteringPointResponsible, marketEvaluationPoint.MeteringPointResponsible.Id);
        Assert.Equal(masterDataContent.Type, marketEvaluationPoint.Type);
        Assert.Equal(masterDataContent.SettlementMethod, marketEvaluationPoint.SettlementMethod);
        Assert.Equal(masterDataContent.MeteringMethod, marketEvaluationPoint.MeteringMethod);
        Assert.Equal(masterDataContent.ConnectionState, marketEvaluationPoint.ConnectionState);
        Assert.Equal(masterDataContent.ReadingPeriodicity, marketEvaluationPoint.ReadCycle);
        Assert.Equal(masterDataContent.NetSettlementGroup, marketEvaluationPoint.NetSettlementGroup);
        Assert.Equal(masterDataContent.ScheduledMeterReadingDate, marketEvaluationPoint.NextReadingDate);
        Assert.Equal(masterDataContent.GridAreaDetails.Code, marketEvaluationPoint.MeteringGridAreaId.Id);
        Assert.Equal(masterDataContent.GridAreaDetails.FromCode, marketEvaluationPoint.OutMeteringGridAreaId.Id);
        Assert.Equal(masterDataContent.GridAreaDetails.ToCode, marketEvaluationPoint.InMeteringGridAreaId.Id);
        Assert.Equal(masterDataContent.PowerPlantGsrnNumber, marketEvaluationPoint.LinkedMarketEvaluationPointId.Id);
        Assert.Equal(
            masterDataContent.Capacity.ToString(CultureInfo.InvariantCulture),
            marketEvaluationPoint.PhysicalConnectionCapacity.Value);
        Assert.Equal(masterDataContent.ConnectionType, marketEvaluationPoint.ConnectionType);
        Assert.Equal(masterDataContent.DisconnectionType, marketEvaluationPoint.DisconnectionMethod);
        Assert.Equal(masterDataContent.AssetType, marketEvaluationPoint.PsrType);
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
        Assert.Equal(masterDataContent.Series.Product, marketEvaluationPoint.Series.Product);
        Assert.Equal(masterDataContent.Series.UnitType, marketEvaluationPoint.Series.QuantityMeasureUnit);
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

    private static IncomingMessageBuilder MessageBuilder()
    {
        return new IncomingMessageBuilder()
            .WithMessageId(SampleData.OriginalMessageId)
            .WithTransactionId(SampleData.TransactionId)
            .WithMarketEvaluationPointId(SampleData.MarketEvaluationPointId);
    }

    private static async Task<OutgoingMessage> GetMessageAsync(IDbConnectionFactory connectionFactory, string documentType)
    {
        if (connectionFactory == null) throw new ArgumentNullException(nameof(connectionFactory));

        var outgoingMessage = await connectionFactory.GetOpenConnection().QuerySingleAsync<OutgoingMessage>(
            $"SELECT [DocumentType], [ReceiverId], [CorrelationId], [OriginalMessageId], [ProcessType], [ReceiverRole], [SenderId], [SenderRole], [MarketActivityRecordPayload],[ReasonCode] FROM b2b.OutgoingMessages WHERE DocumentType = @DocumentType",
            new
            {
                DocumentType = documentType,
            }).ConfigureAwait(false);

        return outgoingMessage;
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
            Guid.NewGuid().ToString());
    }

    private async Task<MarketActivityRecord> GetMarketActivityRecordAsync(string documentType)
    {
        var parser = GetService<IMarketActivityRecordParser>();
        var message = await GetMessageAsync(GetService<IDbConnectionFactory>(), documentType).ConfigureAwait(false);
        var marketActivityRecord =
            parser.From<Messaging.Application.OutgoingMessages.AccountingPointCharacteristics.MarketActivityRecord>(
                message!.MarketActivityRecordPayload);
        return marketActivityRecord;
    }

    private async Task SetupAnAcceptedMoveInTransaction()
    {
        await InvokeCommandAsync(MessageBuilder().Build()).ConfigureAwait(false);
    }
}
