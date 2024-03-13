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
    public void Translate_measurement_unit(string measurementUnit, string expectedCode)
    {
        Assert.Equal(expectedCode, MeasurementUnit.From(measurementUnit).Code);
    }

    [Theory]
    [InlineData(nameof(Resolution.Hourly), "PT1H")]
    [InlineData(nameof(Resolution.QuarterHourly), "PT15M")]
    public void Translate_resolution(string resolution, string expectedCode)
    {
        Assert.Equal(expectedCode, Resolution.From(resolution).Code);
    }

    [Theory]
    [InlineData("1234567890123", "A10")]
    [InlineData("1234567890123456", "A01")]
    public void Translate_actor_number_coding_scheme(string actorNumber, string expectedCode)
    {
        Assert.Equal(expectedCode, CimCode.CodingSchemeOf(ActorNumber.Create(actorNumber)));
    }
}
