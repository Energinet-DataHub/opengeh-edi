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
using System.Reflection;
using Energinet.DataHub.EDI.B2CWebApi.Factories;
using Energinet.DataHub.EDI.B2CWebApi.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using FluentAssertions;
using FluentAssertions.Execution;
using NodaTime;
using Xunit;

namespace Energinet.DataHub.EDI.Tests.B2CWebApi.RequestWholesaleSettlement;

[SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "Test class")]
public class RequestWholesaleSettlementFactoryTests
{
    private readonly DateTimeZone _dateTimeZone = DateTimeZoneProviders.Tzdb["Europe/Copenhagen"];

    public static IReadOnlyCollection<object[]> GetAllCalculationTypes()
    {
        return Enum.GetValues<CalculationType>().Select(v => new object[] { v }).ToList();
    }

    public static IReadOnlyCollection<object[]> GetValidActorRoles()
    {
        return
        [
            [MarketRole.EnergySupplier.Name],
            [MarketRole.GridAccessProvider.Name],
            [MarketRole.SystemOperator.Name],
        ];
    }

    public static IReadOnlyCollection<object[]> GetInvalidActorRoles()
    {
        var validActorRoles = GetValidActorRoles().SelectMany(a => a);

        var invalidActorRoles = EnumerationType.GetAll<MarketRole>()
            .Select(r => r.Name)
            .Where(v => !validActorRoles.Contains(v!))
            .Select(v => new[] { v! })
            .ToList();

        return invalidActorRoles;
    }

    [Theory]
    [InlineData(null, null, null)]
    [InlineData(PriceType.TariffSubscriptionAndFee, null, null)]
    [InlineData(PriceType.Tariff, null, "D03")]
    [InlineData(PriceType.Subscription, null, "D01")]
    [InlineData(PriceType.Fee, null, "D02")]
    [InlineData(PriceType.MonthlyTariff, "P1M", "D03")]
    [InlineData(PriceType.MonthlySubscription, "P1M", "D01")]
    [InlineData(PriceType.MonthlyFee, "P1M", "D02")]
    [InlineData(PriceType.MonthlyTariffSubscriptionAndFee, "P1M", null)]
    public void Given_ValidRequestWithGivenPricetype_When_DtoFactoryInvoked_Then_CorrectDtoProduced(PriceType? priceType, string? expectedResolution, string? expectedChargeType)
    {
        var senderId = "9876543210987";
        var startDay = 10;
        var endDay = 12;

        var request = new RequestWholesaleSettlementMarketRequest(
            CalculationType.WholesaleFixing,
            $"2023-10-{startDay}T22:00:00.000Z",
            $"2023-10-{endDay}T21:59:59.999Z",
            "803",
            "579000000003042",
            null,
            priceType);

        var result = RequestWholesaleSettlementDtoFactory.Create(
            request,
            senderId,
            MarketRole.GridAccessProvider.Name,
            _dateTimeZone,
            SystemClock.Instance.GetCurrentInstant());

        using var assertionScope = new AssertionScope();
        result.BusinessReason.Should().Be("D05");
        result.SenderNumber.Should().Be(senderId);
        result.SenderRoleCode.Should().Be(MarketRole.GridAccessProvider.Code);
        result.MessageType.Should().Be("D21");

        result.Series.Should()
            .AllSatisfy(
                s =>
                {
                    s.Resolution.Should().Be(expectedResolution);
                    s.ChargeOwner.Should().BeNull();
                    s.SettlementVersion.Should().BeNull();

                    s.StartDateAndOrTimeDateTime.Should().Be("2023-10-10T22:00:00Z");
                    s.EndDateAndOrTimeDateTime.Should().Be("2023-10-12T22:00:00Z");

                    if (string.IsNullOrEmpty(expectedChargeType))
                    {
                        s.ChargeTypes.Should()
                            .BeEmpty();
                    }
                    else
                    {
                        s.ChargeTypes
                            .Should()
                            .ContainSingle()
                            .And
                            .AllSatisfy(
                                (c) =>
                                {
                                    c.Id.Should().BeNull();
                                    c.Type.Should().Be(expectedChargeType);
                                });
                    }
                });
    }

    [Fact]
    public void Give_RequestWithIncorrectEndDate_When_DtoFactoryInvoked_Then_DtoWithCorrectedEndDateProduced()
    {
        var request = new RequestWholesaleSettlementMarketRequest(
            CalculationType.WholesaleFixing,
            "2023-10-10T22:00:00.000Z",
            "2023-10-12T21:59:59.998Z",
            "803",
            "579000000003042",
            null,
            null);

        var result = RequestWholesaleSettlementDtoFactory.Create(
            request,
            "9876543210987",
            MarketRole.SystemOperator.Name,
            _dateTimeZone,
            SystemClock.Instance.GetCurrentInstant());

        result.Should().NotBeNull();
        // We had "2023-10-12T21:59:59.998Z" in the request. As this is not the correct end of day (we need 999ms),
        // the factory will ignore this last day (i.e. the 12th) and set the end date to the previous day (i.e. the 11th)
        result.Series.Should().ContainSingle().Subject.EndDateAndOrTimeDateTime.Should().Be("2023-10-11T22:00:00Z");
    }

    [Theory]
    [MemberData(nameof(GetAllCalculationTypes))]
    public void Given_ValidRequestWithSpecificCalculationType_When_DtoFactoryInvoked_Then_DtoProduced(
        CalculationType calculationType)
    {
        var request = GetRequestWithCalculationType(calculationType);

        var result = RequestWholesaleSettlementDtoFactory.Create(
            request,
            "9876543210987",
            MarketRole.GridAccessProvider.Name,
            _dateTimeZone,
            SystemClock.Instance.GetCurrentInstant());

        result.Should().NotBeNull();
        result.BusinessReason.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [MemberData(nameof(GetValidActorRoles))]
    public void Given_ValidRequestWithValidActorRole_When_DtoFactoryInvoked_Then_DtoProduced(string actorRole)
    {
        var request = GetRequestWithCalculationType(CalculationType.WholesaleFixing);

        var result = RequestWholesaleSettlementDtoFactory.Create(
            request,
            "9876543210987",
            actorRole,
            _dateTimeZone,
            SystemClock.Instance.GetCurrentInstant());

        result.Should().NotBeNull();
    }

    [Theory]
    [MemberData(nameof(GetInvalidActorRoles))]
    public void Given_ValidRequestWithInvalidActorRole_When_DtoFactoryInvoked_Then_ThrowsException(string actorRole)
    {
        var request = GetRequestWithCalculationType(CalculationType.WholesaleFixing);

        var act = () => RequestWholesaleSettlementDtoFactory.Create(
            request,
            "9876543210987",
            actorRole,
            _dateTimeZone,
            SystemClock.Instance.GetCurrentInstant());

        act.Should().ThrowExactly<ArgumentException>().WithMessage("roleName: *. is unsupported to map to a role name");
    }

    private RequestWholesaleSettlementMarketRequest GetRequestWithCalculationType(CalculationType calculationType)
    {
        return new RequestWholesaleSettlementMarketRequest(
            calculationType,
            "2023-10-10T22:00:00.000Z",
            "2023-10-12T21:59:59.999Z",
            "803",
            "579000000003042",
            null,
            null);
    }
}
