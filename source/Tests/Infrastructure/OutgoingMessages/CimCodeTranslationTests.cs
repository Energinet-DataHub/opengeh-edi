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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.MarketDocuments;
using Xunit;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages;

public class CimCodeTests
{
    [Theory]
    [InlineData(nameof(BusinessReason.BalanceFixing), "D04")]
    [InlineData(nameof(BusinessReason.MoveIn), "E65")]
    [InlineData(nameof(BusinessReason.PreliminaryAggregation), "D03")]
    [InlineData(nameof(BusinessReason.WholesaleFixing), "D05")]
    [InlineData(nameof(BusinessReason.Correction), "D32")]
    public void Translate_business_reason(string businessReason, string expectedCode)
    {
        Assert.Equal(expectedCode, BusinessReason.FromName(businessReason).Code);
    }

    [Theory]
    [InlineData(nameof(MeteringPointType.Production), "E18")]
    [InlineData(nameof(MeteringPointType.Consumption), "E17")]
    [InlineData(nameof(MeteringPointType.Exchange), "E20")]
    public void Translate_metering_point_type(string meteringPointType, string expectedCode)
    {
        Assert.Equal(expectedCode, MeteringPointType.From(meteringPointType).Code);
    }

    [Theory]
    [InlineData(nameof(ActorRole.MeteredDataResponsible), "MDR")]
    [InlineData(nameof(ActorRole.MeteredDataAdministrator), "DGL")]
    [InlineData(nameof(ActorRole.GridOperator), "DDM")]
    [InlineData(nameof(ActorRole.BalanceResponsibleParty), "DDK")]
    [InlineData(nameof(ActorRole.EnergySupplier), "DDQ")]
    [InlineData(nameof(ActorRole.MeteringPointAdministrator), "DDZ")]
    [InlineData(nameof(ActorRole.ImbalanceSettlementResponsible), "DDX")]
    [InlineData(nameof(ActorRole.SystemOperator), "EZ")]
    [InlineData(nameof(ActorRole.DanishEnergyAgency), "STS")]
    [InlineData(nameof(ActorRole.Delegated), "DEL")]
    public void Translate_market_role(string marketRole, string expectedCode)
    {
        Assert.Equal(expectedCode, EnumerationType.FromName<ActorRole>(marketRole).Code);
    }

    [Theory]
    [InlineData(nameof(SettlementType.NonProfiled), "E02")]
    [InlineData(nameof(SettlementType.Flex), "D01")]
    public void Translate_settlement_type(string settlementType, string expectedCode)
    {
        Assert.Equal(expectedCode, SettlementType.From(settlementType).Code);
    }

    [Theory]
    [InlineData(nameof(MeasurementUnit.Kwh), "KWH")]
    [InlineData(nameof(MeasurementUnit.Pieces), "H87")]
    public void Translate_measurement_unit(string measurementUnit, string expectedCode)
    {
        Assert.Equal(expectedCode, MeasurementUnit.FromName(measurementUnit).Code);
    }

    [Theory]
    [InlineData(nameof(Resolution.QuarterHourly), "PT15M")]
    [InlineData(nameof(Resolution.Daily), "P1D")]
    [InlineData(nameof(Resolution.Hourly), "PT1H")]
    [InlineData(nameof(Resolution.Monthly), "P1M")]
    public void Translate_resolution(string resolution, string expectedCode)
    {
        Assert.Equal(expectedCode, Resolution.From(resolution).Code);
    }

    [Theory]
    [InlineData(nameof(SettlementVersion.FirstCorrection), "D01")]
    [InlineData(nameof(SettlementVersion.SecondCorrection), "D02")]
    [InlineData(nameof(SettlementVersion.ThirdCorrection), "D03")]
    [InlineData(nameof(SettlementVersion.FourthCorrection), "D04")]
    [InlineData(nameof(SettlementVersion.FifthCorrection), "D05")]
    [InlineData(nameof(SettlementVersion.SixthCorrection), "D06")]
    [InlineData(nameof(SettlementVersion.SeventhCorrection), "D07")]
    [InlineData(nameof(SettlementVersion.EighthCorrection), "D08")]
    [InlineData(nameof(SettlementVersion.NinthCorrection), "D09")]
    [InlineData(nameof(SettlementVersion.TenthCorrection), "D10")]
    public void Translate_settlement_version(string settlementVersion, string expectedCode)
    {
        Assert.Equal(expectedCode, SettlementVersion.FromName(settlementVersion).Code);
    }

    [Theory]
    [InlineData(nameof(ChargeType.Subscription), "D01")]
    [InlineData(nameof(ChargeType.Fee), "D02")]
    [InlineData(nameof(ChargeType.Tariff), "D03")]
    public void Translate_charge_type(string chargeType, string expectedCode)
    {
        Assert.Equal(expectedCode, ChargeType.FromName(chargeType).Code);
    }

    [Theory]
    [InlineData(nameof(ProductType.EnergyActive), "8716867000030")]
    [InlineData(nameof(ProductType.Tariff), "5790001330590")]
    public void Translate_product_type(string productType, string expectedCode)
    {
        Assert.Equal(expectedCode, ProductType.FromName(productType).Code);
    }

    [Theory]
    [InlineData(nameof(ReasonCode.FullyAccepted), "A01")]
    [InlineData(nameof(ReasonCode.FullyRejected), "A02")]
    public void Translate_reason_code(string reasonCode, string expectedCode)
    {
        Assert.Equal(expectedCode, ReasonCode.From(reasonCode).Code);
    }

    [Theory]
    [InlineData(nameof(Currency.DanishCrowns), "DKK")]
    public void Translate_currency(string currency, string expectedCode)
    {
        Assert.Equal(expectedCode, Currency.FromName(currency).Code);
    }

    [Theory]
    [InlineData("1234567890123", "A10")]
    [InlineData("1234567890123456", "A01")]
    public void Translate_actor_number_coding_scheme(string actorNumber, string expectedCode)
    {
        Assert.Equal(expectedCode, CimCode.CodingSchemeOf(ActorNumber.Create(actorNumber)));
    }
}
