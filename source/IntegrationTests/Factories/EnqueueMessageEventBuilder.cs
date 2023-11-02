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
using Energinet.DataHub.EDI.ActorMessageQueue.Contracts;
using Energinet.DataHub.EDI.Common;
using Energinet.DataHub.EDI.Common.Actors;
using Energinet.DataHub.EDI.Domain.GridAreas;
using Energinet.DataHub.EDI.IntegrationTests.Assertions;
using Energinet.DataHub.EDI.Process.Domain;
using Energinet.DataHub.EDI.Process.Domain.Transactions;
using Energinet.DataHub.EDI.Process.Domain.Transactions.Aggregations;
using Energinet.DataHub.EDI.Process.Domain.Transactions.Aggregations.OutgoingMessage;
using NodaTime;
using NodaTime.Extensions;
using Period = Energinet.DataHub.EDI.Common.Period;

namespace Energinet.DataHub.EDI.IntegrationTests.Factories;

public class EnqueueMessageEventBuilder
{
    private static readonly Guid _processId = ProcessId.Create(Guid.NewGuid()).Id;
    private static readonly BusinessReason _businessReason = BusinessReason.BalanceFixing;
    private static readonly GridArea _gridAreaDetails = new("805", Instant.FromDateTimeUtc(DateTime.UtcNow), ActorNumber.Create("1234567891045"));
    private static readonly IReadOnlyList<Process.Domain.Transactions.Aggregations.OutgoingMessage.Point> _points = new List<Process.Domain.Transactions.Aggregations.OutgoingMessage.Point>();
    private static ActorNumber _receiverNumber = ActorNumber.Create("1234567891912");
    private static MarketRole _receiverRole = MarketRole.MeteringDataAdministrator;

#pragma warning disable CA1822
    public EnqueueMessageEvent Build()
#pragma warning restore CA1822
    {
        var message = AggregationResultMessage.Create(
            _receiverNumber,
            _receiverRole,
            _processId,
            _gridAreaDetails.GridAreaCode,
            MeteringPointType.Consumption.Name,
            SettlementType.NonProfiled.Name,
            MeasurementUnit.Kwh.Name,
            Resolution.Hourly.Name,
            null,
            "1234567891911",
            new Period(DateTimeOffset.UtcNow.ToInstant(), DateTimeOffset.UtcNow.AddHours(1).ToInstant()),
            _points,
            _businessReason.Name);
        return new EnqueueMessageEvent(message);
    }

    public EnqueueMessageEventBuilder WithReceiverNumber(string receiverNumber)
    {
        _receiverNumber = ActorNumber.Create(receiverNumber);
        return this;
    }

    public EnqueueMessageEventBuilder WithReceiverRole(MarketRole marketRole)
    {
        _receiverRole = marketRole;
        return this;
    }
}

internal sealed record MessageRecordStub();
