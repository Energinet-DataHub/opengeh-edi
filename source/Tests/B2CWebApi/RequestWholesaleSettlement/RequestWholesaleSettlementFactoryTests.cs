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

    [Fact]
    public void Given_ValidRequest_When_DtoFactoryInvoked_Then_CorrectDtoProduced()
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
            "PT15M",
            "889400000000312",
            [
                new RequestWholesaleSettlementChargeType("1024", "SomeType1"),
                new RequestWholesaleSettlementChargeType("512", "SomeType2"),
            ]);

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
                    s.Resolution.Should().Be("PT15M");
                    s.ChargeOwner.Should().Be("889400000000312");
                    s.SettlementVersion.Should().BeNull();

                    s.StartDateAndOrTimeDateTime.Should().Be("2023-10-10T22:00:00Z");
                    s.EndDateAndOrTimeDateTime.Should().Be("2023-10-12T22:00:00Z");

                    s.ChargeTypes.Should()
                        .SatisfyRespectively(
                            ct =>
                            {
                                ct.Id.Should().Be("1024");
                                ct.Type.Should().Be("SomeType1");
                            },
                            ct =>
                            {
                                ct.Id.Should().Be("512");
                                ct.Type.Should().Be("SomeType2");
                            });
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
            "PT15M",
            "889400000000312",
            []);

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
            "PT15M",
            "889400000000312",
            [
                new RequestWholesaleSettlementChargeType("1024", "SomeType1"),
                new RequestWholesaleSettlementChargeType("512", "SomeType2"),
            ]);
    }
}
