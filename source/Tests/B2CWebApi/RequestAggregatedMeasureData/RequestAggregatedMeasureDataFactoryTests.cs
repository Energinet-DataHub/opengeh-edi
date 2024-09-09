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

using Energinet.DataHub.EDI.B2CWebApi.Factories;
using Energinet.DataHub.EDI.B2CWebApi.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.EntityFrameworkCore.SqlServer.NodaTime.Extensions;
using NodaTime;
using NodaTime.Text;
using Xunit;
using MeteringPointType = Energinet.DataHub.EDI.B2CWebApi.Models.MeteringPointType;

namespace Energinet.DataHub.EDI.Tests.B2CWebApi.RequestAggregatedMeasureData;

public class RequestAggregatedMeasureDataFactoryTests
{
    private const string SenderId = "9876543210987";
    private const int StartDay = 10;
    private const int EndDay = 12;
    private readonly DateTimeZone _dateTimeZone = DateTimeZoneProviders.Tzdb["Europe/Copenhagen"];

    [Fact]
    public void Can_create_RequestAggregatedMeasureData_with_correct_data()
    {
        var request = RequestAggregatedMeasureDataMarketRequest();

        var result = RequestAggregatedMeasureDataDtoFactory.Create(
            request,
            SenderId,
            MarketRole.MeteredDataResponsible.Name,
            _dateTimeZone,
            SystemClock.Instance.GetCurrentInstant());

        using var assertionScope = new AssertionScope();
        Assert.Equal("D04", result.BusinessReason);
        Assert.Equal(SenderId, result.SenderNumber);
        Assert.Equal(MarketRole.MeteredDataResponsible.Code, result.SenderRoleCode);
        Assert.Equal("E74", result.MessageType);

        Assert.All(result.Serie, serie =>
        {
            Assert.Equal("E18", serie.MarketEvaluationPointType);
            Assert.Null(serie.MarketEvaluationSettlementMethod);
            Assert.Null(serie.SettlementVersion);

            var startDate = InstantPattern.General.Parse(serie.StartDateAndOrTimeDateTime).GetValueOrThrow();
            Assert.Equal(StartDay, startDate.Day());
            var endDate = InstantPattern.General.Parse(serie.EndDateAndOrTimeDateTime!).GetValueOrThrow();
            Assert.Equal(EndDay, endDate.Day());
        });
    }

    [Fact]
    public void Can_create_RequestAggregatedMeasureData_wrong_endData_input()
    {
        var request = RequestAggregatedMeasureDataMarketRequest(998);

        var result = RequestAggregatedMeasureDataDtoFactory.Create(
            request,
            "9876543210987",
            MarketRole.MeteredDataResponsible.Name,
            _dateTimeZone,
            SystemClock.Instance.GetCurrentInstant());

        var endDate = InstantPattern.General.Parse(result.Serie.First().EndDateAndOrTimeDateTime!).GetValueOrThrow();
        Assert.Equal(EndDay - 1, endDate.Day());
    }

    [Fact]
    public void RequestingRequestAggregatedMeasureData_AsGridOperator_RequestingRoleIsChangedToMeteredDataResponsible()
    {
        var request = RequestAggregatedMeasureDataMarketRequest();

        var result = RequestAggregatedMeasureDataDtoFactory.Create(
            request,
            "9876543210987",
            MarketRole.GridAccessProvider.Name,
            _dateTimeZone,
            SystemClock.Instance.GetCurrentInstant());

        result.ReceiverRoleCode.Should().Be(ActorRole.MeteredDataAdministrator.Code);
    }

    private static RequestAggregatedMeasureDataMarketRequest RequestAggregatedMeasureDataMarketRequest(int milliseconds = 999)
    {
        var request = new RequestAggregatedMeasureDataMarketRequest(
            CalculationType: CalculationType.BalanceFixing,
            MeteringPointType: MeteringPointType.Production,
            StartDate: $"2023-10-{StartDay}T22:00:00.000Z",
            EndDate: $"2023-10-{EndDay}T21:59:59.{milliseconds}Z",
            GridArea: "803",
            EnergySupplierId: "579000000003042",
            BalanceResponsibleId: "1234567890123");
        return request;
    }
}
