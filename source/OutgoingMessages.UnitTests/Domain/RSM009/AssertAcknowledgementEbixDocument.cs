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
using Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.Asserts;
using Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.NotifyWholesaleServices;
using NodaTime;

namespace Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.RSM009;

public class AssertAcknowledgementEbixDocument : IAssertAcknowledgementDocument
{
    private readonly AssertEbixDocument _documentAsserter;
    private readonly bool _skipIdentificationLengthValidation;

    public AssertAcknowledgementEbixDocument(AssertEbixDocument documentAsserter, bool skipIdentificationLengthValidation = false)
    {
        _documentAsserter = documentAsserter;
        _skipIdentificationLengthValidation = skipIdentificationLengthValidation;
        _documentAsserter.HasValue("HeaderEnergyDocument/DocumentType", "294");
        _documentAsserter.HasValue("PayloadResponseEvent[1]/StatusType", "41");

        _documentAsserter.HasValueWithAttributes(
            "ProcessEnergyContext/EnergyIndustryClassification",
            "23",
            CreateRequiredListAttributes(CodeListType.UnitedNations));
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
        _documentAsserter.HasValueWithAttributes(
            "ProcessEnergyContext/EnergyBusinessProcessRole",
            EbixCode.Of(receiverRole),
            CreateRequiredListAttributes(CodeListType.Ebix));
        return this;
    }

    public IAssertAcknowledgementDocument HasReceivedBusinessReasonCode(BusinessReason businessReason)
    {
        _documentAsserter.HasValueWithAttributes(
            "ProcessEnergyContext/EnergyBusinessProcess",
            EbixCode.Of(businessReason),
            CreateRequiredListAttributes(businessReason.Code.StartsWith('D') ? CodeListType.EbixDenmark : CodeListType.Ebix));
        return this;
    }

    public IAssertAcknowledgementDocument HasRelatedToMessageId(MessageId originalMessageId)
    {
        _documentAsserter.HasValue("ProcessEnergyContext/OriginalBusinessMessage", originalMessageId.Value);
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
            "PayloadResponseEvent[1]/OriginalBusinessDocument",
            originalTransactionId.Value);
        return this;
    }

    public IAssertAcknowledgementDocument HasTransactionId(TransactionId transactionId)
    {
        _documentAsserter.HasValue(
            "PayloadResponseEvent[1]/Identification",
            transactionId.Value);
        return this;
    }

    public IAssertAcknowledgementDocument SeriesHasReasons(params RejectReason[] rejectReasons)
    {
        _documentAsserter.HasValue(
            "PayloadResponseEvent[1]/ResponseReasonType",
            rejectReasons.First().ErrorCode);
        _documentAsserter.HasValue(
            "PayloadResponseEvent[1]/ReasonText",
            rejectReasons.First().ErrorMessage);

        _documentAsserter.HasValue(
            "PayloadResponseEvent[2]/ResponseReasonType",
            null!);
        _documentAsserter.HasValue(
            "PayloadResponseEvent[2]/ReasonText",
            null!);
        return this;
    }

    private static AttributeNameAndValue[] CreateRequiredListAttributes(CodeListType codeListType)
    {
        var (codeList, countryCode) = GetCodeListConstant(codeListType);

        var requiredAttributes = new List<AttributeNameAndValue> { new("listAgencyIdentifier", codeList), };

        if (!string.IsNullOrEmpty(countryCode))
            requiredAttributes.Add(new("listIdentifier", countryCode));

        return requiredAttributes.ToArray();
    }

    private static (string CodeList, string? CountryCode) GetCodeListConstant(CodeListType codeListType) =>
        codeListType switch
        {
            CodeListType.UnitedNations => (EbixDocumentWriter.UnitedNationsCodeList, null),
            CodeListType.Ebix => (EbixDocumentWriter.EbixCodeList, null),
            CodeListType.EbixDenmark => (EbixDocumentWriter.EbixCodeList, EbixDocumentWriter.CountryCodeDenmark),
            _ => throw new ArgumentOutOfRangeException(nameof(codeListType), codeListType, "Invalid CodeListType"),
        };
}
