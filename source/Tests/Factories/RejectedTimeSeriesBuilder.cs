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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages;
using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData.OutgoingMessages;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.RejectRequestAggregatedMeasureData;
using NodaTime;

namespace Energinet.DataHub.EDI.Tests.Factories;

public class RejectedTimeSeriesBuilder
{
    private readonly string _receiverNumber = SampleData.ReceiverId;
    private readonly ActorRole _receiverRole = SampleData.ReceiverRole;
    private readonly string _senderNumber = SampleData.SenderId;
    private readonly ActorRole _senderRole = SampleData.SenderRole;
    private readonly string _messageId = SampleData.MessageId;
    private readonly BusinessReason _businessReason = SampleData.BusinessReason;
    private readonly Instant _creationDate = SampleData.CreationDate;
    private readonly Guid _transactionId = SampleData.TransactionId;
    private readonly string _originalTransactionIdReference = SampleData.OriginalTransactionId;
    private readonly IReadOnlyCollection<RejectReason> _rejectReasons = new List<RejectReason> { new(SampleData.SerieReasonCode, SampleData.SerieReasonMessage) };

    public static RejectedTimeSeriesBuilder RejectAggregatedMeasureDataResult()
    {
        return new RejectedTimeSeriesBuilder();
    }

    public OutgoingMessageHeader BuildHeader()
    {
        return new OutgoingMessageHeader(
            _businessReason.Name,
            _senderNumber,
            _senderRole.Code,
            _receiverNumber,
            _receiverRole.Code,
            _messageId,
            _creationDate);
    }

    public RejectedTimeSerie BuildRejectedTimeSerie()
    {
        return new RejectedTimeSerie(
            _transactionId,
            _rejectReasons,
            _originalTransactionIdReference);
    }
}
