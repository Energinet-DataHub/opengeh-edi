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
using System.Threading.Tasks;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Application.MasterData;
using Messaging.Application.Transactions.MoveIn;
using Messaging.IntegrationTests.Application.IncomingMessages;
using Messaging.IntegrationTests.Fixtures;
using Processing.IntegrationTests.Application;
using Xunit;

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

    private static IncomingMessageBuilder MessageBuilder()
    {
        return new IncomingMessageBuilder()
            .WithMessageId(SampleData.OriginalMessageId)
            .WithTransactionId(SampleData.TransactionId);
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
            MasterDataSampleData.ConnectionType);
    }

    private async Task SetupAnAcceptedMoveInTransaction()
    {
        await InvokeCommandAsync(MessageBuilder().Build()).ConfigureAwait(false);
    }
}
