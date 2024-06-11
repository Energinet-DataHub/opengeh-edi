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

using System.Linq;
using Energinet.DataHub.EDI.B2CWebApi.Factories;
using Energinet.DataHub.EDI.B2CWebApi.Models;
using FluentAssertions.Execution;
using Microsoft.EntityFrameworkCore.SqlServer.NodaTime.Extensions;
using NodaTime;
using NodaTime.Text;
using Xunit;

namespace Energinet.DataHub.EDI.Tests.B2CWebApi.RequestAggregatedMeasureData;

public class RequestAggregatedMeasureDataFactoryTests
{
    private readonly DateTimeZone _dateTimeZone = DateTimeZoneProviders.Tzdb["Europe/Copenhagen"];

    [Fact]
    public void Can_create_RequestAggregatedMeasureData_with_correct_data()
    {
        var senderId = "9876543210987";
        var startDay = 10;
        var endDay = 12;
        var request = new RequestAggregatedMeasureDataMarketRequest(
            CalculationType: CalculationType.BalanceFixing,
            MeteringPointType: MeteringPointType.Production,
            StartDate: $"2023-10-{startDay}T22:00:00.000Z",
            EndDate: $"2023-10-{endDay}T21:59:59.999Z",
            GridArea: "803",
            EnergySupplierId: "579000000003042",
            BalanceResponsibleId: "1234567890123");

        var result = RequestAggregatedMeasureDataDtoFactory.Create(
            request,
            senderId,
            MarketRole.MeteredDataResponsible.Name,
            _dateTimeZone,
            SystemClock.Instance.GetCurrentInstant());

        using var assertionScope = new AssertionScope();
        Assert.Equal("D04", result.BusinessReason);
        Assert.Equal(senderId, result.SenderNumber);
        Assert.Equal(MarketRole.MeteredDataResponsible.Code, result.SenderRoleCode);
        Assert.Equal("E74", result.MessageType);

        Assert.All(result.Serie, serie =>
        {
            Assert.Equal("E18", serie.MarketEvaluationPointType);
            Assert.Null(serie.MarketEvaluationSettlementMethod);
            Assert.Null(serie.SettlementVersion);

            var startDate = InstantPattern.General.Parse(serie.StartDateAndOrTimeDateTime).GetValueOrThrow();
            Assert.Equal(startDay, startDate.Day());
            var endDate = InstantPattern.General.Parse(serie.EndDateAndOrTimeDateTime!).GetValueOrThrow();
            Assert.Equal(endDay, endDate.Day());
        });
    }

    [Fact]
    public void Can_create_RequestAggregatedMeasureData_wrong_endData_input()
    {
        var endDay = 12;
        var endDataMilliseconds = 998;
        var request = new RequestAggregatedMeasureDataMarketRequest(
            CalculationType: CalculationType.BalanceFixing,
            MeteringPointType: MeteringPointType.Production,
            StartDate: $"2023-10-10T22:00:00.000Z",
            EndDate: $"2023-10-{endDay}T21:59:59.{endDataMilliseconds}Z",
            GridArea: "803",
            EnergySupplierId: "579000000003042",
            BalanceResponsibleId: "1234567890123");

        var result = RequestAggregatedMeasureDataDtoFactory.Create(
            request,
            "9876543210987",
            MarketRole.MeteredDataResponsible.Name,
            _dateTimeZone,
            SystemClock.Instance.GetCurrentInstant());

        var endDate = InstantPattern.General.Parse(result.Serie.First().EndDateAndOrTimeDateTime!).GetValueOrThrow();
        Assert.Equal(endDay - 1, endDate.Day());
    }
}
