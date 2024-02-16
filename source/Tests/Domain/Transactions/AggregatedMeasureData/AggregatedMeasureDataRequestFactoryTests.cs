﻿// Copyright 2020 Energinet DataHub A/S
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
using Energinet.DataHub.EDI.B2CWebApi.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.Process.Application.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Process.Domain.Transactions;
using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Process.Infrastructure.Transactions.AggregatedMeasureData;
using Energinet.DataHub.Edi.Requests;
using FluentAssertions;
using Xunit;
using MeteringPointType = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.MeteringPointType;

namespace Energinet.DataHub.EDI.Tests.Domain.Transactions.AggregatedMeasureData;

public sealed class AggregatedMeasureDataRequestFactoryTests
{
    [Fact]
    public void With_metering_point_type()
    {
        var process = new AggregatedMeasureDataProcess(
            ProcessId.New(),
            BusinessTransactionId.Create(Guid.NewGuid().ToString()),
            ActorNumber.Create("8200000007743"),
            MarketRole.BalanceResponsibleParty.Code,
            BusinessReason.BalanceFixing,
            MeteringPointType.Production.Code,
            null,
            "123456",
            "123457",
            "42",
            null,
            null,
            null);

        // Act
        var serviceBusMessage = AggregatedMeasureDataRequestFactory.CreateServiceBusMessage(process);

        // Assert
        serviceBusMessage.Should().NotBeNull();

        var request = AggregatedTimeSeriesRequest.Parser.ParseFrom(serviceBusMessage.Body);
        request.MeteringPointType.Should().Be(MeteringPointType.Production.Code);
    }

    [Fact]
    public void Without_metering_point_type()
    {
        var process = new AggregatedMeasureDataProcess(
            ProcessId.New(),
            BusinessTransactionId.Create(Guid.NewGuid().ToString()),
            ActorNumber.Create("8200000007743"),
            MarketRole.BalanceResponsibleParty.Code,
            BusinessReason.BalanceFixing,
            null,
            null,
            "123456",
            "123457",
            "42",
            null,
            null,
            null);

        // Act
        var serviceBusMessage = AggregatedMeasureDataRequestFactory.CreateServiceBusMessage(process);

        // Assert
        serviceBusMessage.Should().NotBeNull();

        var request = AggregatedTimeSeriesRequest.Parser.ParseFrom(serviceBusMessage.Body);
        request.MeteringPointType.Should().BeEmpty();
    }
}
