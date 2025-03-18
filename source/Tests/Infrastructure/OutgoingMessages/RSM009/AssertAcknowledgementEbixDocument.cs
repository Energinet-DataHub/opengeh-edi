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
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats.Ebix;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.MeteredDataForMeteringPoint;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.Asserts;
using NodaTime;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.RSM009;

public class AssertAcknowledgementEbixDocument : IAssertAcknowledgementDocument
{
    private readonly AssertEbixDocument _documentAsserter;
    private readonly bool _skipIdentificationLengthValidation;

    public AssertAcknowledgementEbixDocument(AssertEbixDocument documentAsserter, bool skipIdentificationLengthValidation = false)
    {
        _documentAsserter = documentAsserter;
        _skipIdentificationLengthValidation = skipIdentificationLengthValidation;
        _documentAsserter.HasValue("HeaderEnergyDocument/DocumentType", null!);
    }

    public IAssertAcknowledgementDocument HasMessageId(MessageId messageId)
    {
        _documentAsserter.HasValue("HeaderEnergyDocument/Identification", messageId.Value);
        return this;
    }

    public IAssertAcknowledgementDocument HasSenderId(ActorNumber senderId)
    {
        _documentAsserter.HasValue("HeaderEnergyDocument/SenderEnergyParty/Identification", senderId.Value);
        return this;
    }

    public IAssertAcknowledgementDocument HasSenderRole(ActorRole senderRole)
    {
        // Ebix doesn't have a sender role?
        return this;
    }

    public IAssertAcknowledgementDocument HasReceiverId(ActorNumber receiverId)
    {
        _documentAsserter.HasValue("HeaderEnergyDocument/RecipientEnergyParty/Identification", receiverId.Value);
        return this;
    }

    public IAssertAcknowledgementDocument HasReceiverRole(ActorRole receiverRole)
    {
        _documentAsserter.HasValue("ProcessEnergyContext/EnergyBusinessProcessRole", EbixCode.Of(receiverRole));
        return this;
    }

    public IAssertAcknowledgementDocument HasReceivedBusinessReasonCode(BusinessReason businessReason)
    {
        _documentAsserter.HasValue("ProcessEnergyContext/EnergyBusinessProcess", EbixCode.Of(businessReason));
        return this;
    }

    public IAssertAcknowledgementDocument HasRelatedToMessageId(MessageId relatedTOriginalMessageId)
    {
        _documentAsserter.HasValue("ProcessEnergyContext/OriginalBusinessMessage", relatedTOriginalMessageId.Value);
        return this;
    }

    public IAssertAcknowledgementDocument HasCreationDate(Instant creationDate)
    {
        _documentAsserter.HasValue("HeaderEnergyDocument/Creation", creationDate.ToString());
        return this;
    }

    public async Task<IAssertAcknowledgementDocument> DocumentIsValidAsync()
    {
        await _documentAsserter.HasValidStructureAsync(DocumentType.Acknowledgement, "3", _skipIdentificationLengthValidation)
            .ConfigureAwait(false);
        return this;
    }

    public IAssertAcknowledgementDocument HasOriginalTransactionId(TransactionId originalTransactionId)
    {
        _documentAsserter.HasValue(
            "PayloadResponseEvent[1]/Identification",
            originalTransactionId.Value);
        return this;
    }

    public IAssertAcknowledgementDocument SeriesHasReasons(params RejectReason[] rejectReasons)
    {
        _documentAsserter.HasValue(
            "PayloadResponseEvent[1]/ResponseReasonType",
            rejectReasons.First().ErrorCode);
        return this;
    }
}
