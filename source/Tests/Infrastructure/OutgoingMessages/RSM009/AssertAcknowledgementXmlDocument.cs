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
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.Asserts;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.RSM009;

public class AssertAcknowledgementXmlDocument : IAssertAcknowledgementDocument
{
    private readonly AssertXmlDocument _documentAsserter;

    public AssertAcknowledgementXmlDocument(AssertXmlDocument documentAsserter)
    {
        _documentAsserter = documentAsserter;
        _documentAsserter.HasValue("type", "ERR");
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

    public IAssertAcknowledgementDocument HasReasonCode(ReasonCode reasonCode)
    {
        _documentAsserter.HasValue("reason.code", reasonCode.Code);
        return this;
    }

    public async Task<IAssertAcknowledgementDocument> DocumentIsValidAsync()
    {
        await _documentAsserter.HasValidStructureAsync(DocumentType.Acknowledgement)
            .ConfigureAwait(false);
        return this;
    }

    public IAssertAcknowledgementDocument HasTransactionId(TransactionId transactionId)
    {
        ArgumentNullException.ThrowIfNull(transactionId);
        _documentAsserter.HasValue("Series[1]/mRID", transactionId.Value);
        return this;
    }

    public IAssertAcknowledgementDocument HasOriginalTransactionId(TransactionId originalTransactionId)
    {
        ArgumentNullException.ThrowIfNull(originalTransactionId);
        _documentAsserter.HasValue(
            "Series[1]/originalTransactionIDReference_Series.mRID",
            originalTransactionId.Value);
        return this;
    }
}
