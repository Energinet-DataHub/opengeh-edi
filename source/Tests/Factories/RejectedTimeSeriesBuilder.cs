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
using Domain.Actors;
using Domain.OutgoingMessages;
using Domain.OutgoingMessages.NotifyAggregatedMeasureData;
using Domain.OutgoingMessages.RejectedRequestAggregatedMeasureData;
using Domain.Transactions.Aggregations;
using NodaTime;
using NodaTime.Text;
using Period = Domain.Transactions.Aggregations.Period;
using Point = Domain.OutgoingMessages.NotifyAggregatedMeasureData.Point;

namespace Tests.Factories;

public class RejectedTimeSeriesBuilder
{
    private readonly RejectReason _rejectReason = new RejectReason("342", "This is an error");
    private readonly string _originalTransactionIdReference = Guid.NewGuid().ToString();
    private readonly string _receiverNumber = "1234567890123";
    private readonly MarketRole _receiverRole = MarketRole.MeteredDataResponsible;
    private readonly string _senderNumber = "1234567890321";
    private readonly MarketRole _senderRole = MarketRole.MeteringDataAdministrator;
    private readonly string _messageId = Guid.NewGuid().ToString();
    private readonly BusinessReason _businessReason = BusinessReason.BalanceFixing;
    private readonly Instant _timeStamp = SystemClock.Instance.GetCurrentInstant();
    private readonly Guid _transactionId = Guid.NewGuid();

    public static RejectedTimeSeriesBuilder RejectAggregatedMeasureDataResult()
    {
        return new RejectedTimeSeriesBuilder();
    }

    // public TimeSeriesBuilder WithTransactionId(Guid transactionId)
    // {
    //     _transactionId = transactionId;
    //     return this;
    // }
    public MessageHeader BuildHeader()
    {
        return new MessageHeader(
            _businessReason.Name,
            _senderNumber,
            _senderRole.Name,
            _receiverNumber,
            _receiverRole.Name,
            _messageId,
            _timeStamp);
    }

    public RejectedTimeSerie BuildRejectedTimeSerie()
    {
        return new RejectedTimeSerie(
            _transactionId,
            _rejectReason,
            _originalTransactionIdReference);
    }

    private static Instant ParseTimeStamp(string timestamp)
    {
        return InstantPattern.General.Parse(timestamp).Value;
    }
}
