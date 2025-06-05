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
using Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.Asserts;
using NodaTime;

namespace Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.RSM015;

public class AssertRejectRequestRequestMeasurementsEbixDocument
    : IAssertRejectRequestMeasurementsDocument
{
    private readonly AssertEbixDocument _documentAsserter;

    public AssertRejectRequestRequestMeasurementsEbixDocument(
        AssertEbixDocument documentAsserter)
    {
        _documentAsserter = documentAsserter;
        _documentAsserter.HasValue("HeaderEnergyDocument/DocumentType", "ERR");
    }

    public async Task<IAssertRejectRequestMeasurementsDocument> DocumentIsValidAsync()
    {
        await _documentAsserter.HasValidStructureAsync(
            DocumentType.RejectRequestMeasurements,
            "3").ConfigureAwait(false);
        return this;
    }

    public IAssertRejectRequestMeasurementsDocument HasBusinessReason(BusinessReason businessReason)
    {
        _documentAsserter.HasValue(
            "ProcessEnergyContext/EnergyBusinessProcess",
            EbixCode.Of(businessReason));
        return this;
    }

    public IAssertRejectRequestMeasurementsDocument HasReasonCode(ReasonCode reasonCode)
    {
        _documentAsserter.HasValue(
            "PayloadResponseEvent[1]/StatusType",
            EbixCode.Of(ReasonCode.FromCode(reasonCode.Code)));
        return this;
    }

    public IAssertRejectRequestMeasurementsDocument HasMessageId(string expectedMessageId)
    {
        _documentAsserter.HasValue("HeaderEnergyDocument/Identification", expectedMessageId);
        return this;
    }

    public IAssertRejectRequestMeasurementsDocument MessageIdExists()
    {
        _documentAsserter.ElementExists("HeaderEnergyDocument/Identification");
        return this;
    }

    public IAssertRejectRequestMeasurementsDocument HasSenderId(ActorNumber expectedSenderId)
    {
        _documentAsserter.HasValue(
            "HeaderEnergyDocument/SenderEnergyParty/Identification",
            expectedSenderId.Value);
        return this;
    }

    // Not present in Ebix
    public IAssertRejectRequestMeasurementsDocument HasSenderRole(ActorRole expectedSenderRole)
    {
        return this;
    }

    public IAssertRejectRequestMeasurementsDocument HasReceiverId(ActorNumber expectedReceiverId)
    {
        _documentAsserter.HasValue(
            "HeaderEnergyDocument/RecipientEnergyParty/Identification",
            expectedReceiverId.Value);
        return this;
    }

    public IAssertRejectRequestMeasurementsDocument HasReceiverRole(ActorRole expectedReceiverRole)
    {
        _documentAsserter.HasValue(
            "ProcessEnergyContext/EnergyBusinessProcessRole",
            expectedReceiverRole.Code);
        return this;
    }

    public IAssertRejectRequestMeasurementsDocument HasTimestamp(Instant expectedTimestamp)
    {
        _documentAsserter.HasValue("HeaderEnergyDocument/Creation", expectedTimestamp.ToString());
        return this;
    }

    public IAssertRejectRequestMeasurementsDocument HasTransactionId(TransactionId expectedTransactionId)
    {
        _documentAsserter.HasValue("PayloadResponseEvent[1]/Identification", expectedTransactionId.Value);
        return this;
    }

    public IAssertRejectRequestMeasurementsDocument TransactionIdExists()
    {
        _documentAsserter.ElementExists("PayloadResponseEvent[1]/Identification");
        return this;
    }

    public IAssertRejectRequestMeasurementsDocument HasOriginalTransactionId(TransactionId expectedOriginalTransactionId)
    {
        _documentAsserter.HasValue(
            "PayloadResponseEvent[1]/OriginalBusinessDocument",
            expectedOriginalTransactionId.Value);
        return this;
    }

    public IAssertRejectRequestMeasurementsDocument HasMeteringPointId(
        MeteringPointId expectedMeteringPointId)
    {
        _documentAsserter.HasValue(
            $"PayloadResponseEvent[1]/MeteringPointDomainLocation/Identification",
            expectedMeteringPointId.Value);
        return this;
    }

    public IAssertRejectRequestMeasurementsDocument HasSerieReasonCode(string expectedSerieReasonCode)
    {
        _documentAsserter.HasValue($"PayloadResponseEvent[1]/ResponseReasonType", expectedSerieReasonCode);
        return this;
    }

    public IAssertRejectRequestMeasurementsDocument HasSerieReasonMessage(string expectedSerieReasonMessage)
    {
        //In ebIX we don't have a field for text information meaning this method should always assert true
        return this;
    }
}
