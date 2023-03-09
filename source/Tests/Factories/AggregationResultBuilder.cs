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
using Domain.Actors;
using Domain.OutgoingMessages;
using NodaTime;

namespace Tests.Factories;

public class AggregationResultBuilder
{
    private readonly string _messageId = Guid.NewGuid().ToString();
    private readonly Instant _timeStamp = SystemClock.Instance.GetCurrentInstant();
    private ProcessType _processType = ProcessType.BalanceFixing;
    private string _receiverNumber = "1234567890123";
    private MarketRole _receiverRole = MarketRole.MeteredDataResponsible;
    private string _senderNumber = "1234567890321";
    private MarketRole _senderRole = MarketRole.MeteringDataAdministrator;

    public static AggregationResultBuilder AggregationResult()
    {
        return new AggregationResultBuilder();
    }

    public AggregationResultBuilder WithProcessType(ProcessType processType)
    {
        _processType = processType;
        return this;
    }

    public AggregationResultBuilder WithReceiver(string receiverNumber, MarketRole marketRole)
    {
        _receiverNumber = receiverNumber;
        _receiverRole = marketRole;
        return this;
    }

    public AggregationResultBuilder WithSender(string senderNumber, MarketRole marketRole)
    {
        _senderNumber = senderNumber;
        _senderRole = marketRole;
        return this;
    }

    public MessageHeader BuildHeader()
    {
        return new MessageHeader(
            _processType.Name,
            _senderNumber,
            _senderRole.Name,
            _receiverNumber,
            _receiverRole.Name,
            _messageId,
            _timeStamp);
    }
}
