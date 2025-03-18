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
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.MeteredDataForMeteringPoint;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.Asserts;
using NodaTime;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.RSM009;

public class AssertAcknowledgementXmlDocument : IAssertAcknowledgementDocument
{
    private readonly AssertXmlDocument _documentAsserter;

    public AssertAcknowledgementXmlDocument(AssertXmlDocument documentAsserter)
    {
        _documentAsserter = documentAsserter;
        _documentAsserter.HasValue("type", null!);
    }

    public IAssertAcknowledgementDocument HasMessageId(MessageId messageId)
    {
        _documentAsserter.HasValue("mRID", messageId.Value);
        return this;
    }

    public IAssertAcknowledgementDocument HasSenderId(ActorNumber senderId)
    {
        _documentAsserter.HasValue("sender_MarketParticipant.mRID", senderId.Value);
        return this;
    }

    public IAssertAcknowledgementDocument HasSenderRole(ActorRole senderRole)
    {
        ArgumentNullException.ThrowIfNull(senderRole);
        _documentAsserter.HasValue("sender_MarketParticipant.marketRole.type", senderRole.Code);
        return this;
    }

    public IAssertAcknowledgementDocument HasReceiverId(ActorNumber receiverId)
    {
        _documentAsserter.HasValue("receiver_MarketParticipant.mRID", receiverId.Value);
        return this;
    }

    public IAssertAcknowledgementDocument HasReceiverRole(ActorRole receiverRole)
    {
        ArgumentNullException.ThrowIfNull(receiverRole);
        _documentAsserter.HasValue("receiver_MarketParticipant.marketRole.type", receiverRole.Code);
        return this;
    }

    public IAssertAcknowledgementDocument HasReceivedBusinessReasonCode(BusinessReason businessReason)
    {
        _documentAsserter.HasValue("received_MarketDocument.process.processType", businessReason.Code);
        return this;
    }

    public IAssertAcknowledgementDocument HasOriginalMessageId(MessageId originalMessageId)
    {
        _documentAsserter.HasValue("received_MarketDocument.mRID", originalMessageId.Value);
        return this;
    }

    public IAssertAcknowledgementDocument HasCreationDate(Instant creationDate)
    {
        _documentAsserter.HasValue("createdDateTime", creationDate.ToString());
        return this;
    }

    public async Task<IAssertAcknowledgementDocument> DocumentIsValidAsync()
    {
        await _documentAsserter.HasValidStructureAsync(DocumentType.Acknowledgement)
            .ConfigureAwait(false);
        return this;
    }

    public IAssertAcknowledgementDocument HasOriginalTransactionId(TransactionId originalTransactionId)
    {
        ArgumentNullException.ThrowIfNull(originalTransactionId);
        _documentAsserter.HasValue(
            "Series[1]/mRID",
            originalTransactionId.Value);
        return this;
    }

    public IAssertAcknowledgementDocument SeriesHasReasons(params RejectReason[] rejectReasons)
    {
        // If this failed, then the document has to many reasons compared to the expected
        _documentAsserter.HasValue($"Series[1]/Reason[{rejectReasons.Length + 1}]", null!);

        for (var i = 0; i < rejectReasons.Length;  i++)
        {
            _documentAsserter.HasValue(
                $"Series[1]/Reason[{i + 1}]/code",
                rejectReasons[i].ErrorCode);
            _documentAsserter.HasValue(
                $"Series[1]/Reason[{i + 1}]/text",
                rejectReasons[i].ErrorMessage);
        }

        return this;
    }
}
