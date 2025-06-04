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
using Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.Asserts;
using NodaTime;

namespace Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.RSM015;

public class AssertRejectRequestRequestMeasurementsXmlDocument : IAssertRejectRequestMeasurementsDocument
{
    private readonly AssertXmlDocument _documentAsserter;

    public AssertRejectRequestRequestMeasurementsXmlDocument(AssertXmlDocument documentAsserter)
    {
        _documentAsserter = documentAsserter;
        _documentAsserter.HasValue("type", "ERR");
    }

    public async Task<IAssertRejectRequestMeasurementsDocument> DocumentIsValidAsync()
    {
        await _documentAsserter.HasValidStructureAsync(DocumentType.RejectRequestMeasurements)
            .ConfigureAwait(false);
        return this;
    }

    public IAssertRejectRequestMeasurementsDocument HasBusinessReason(BusinessReason businessReason)
    {
        _documentAsserter.HasValue("process.processType", businessReason.Code);
        return this;
    }

    public IAssertRejectRequestMeasurementsDocument HasReasonCode(ReasonCode reasonCode)
    {
        _documentAsserter.HasValue("reason.code", reasonCode.Code);
        return this;
    }

    public IAssertRejectRequestMeasurementsDocument HasMessageId(string expectedMessageId)
    {
        _documentAsserter.HasValue("mRID", expectedMessageId);
        return this;
    }

    public IAssertRejectRequestMeasurementsDocument MessageIdExists()
    {
        _documentAsserter.ElementExists("mRID");
        return this;
    }

    public IAssertRejectRequestMeasurementsDocument HasSenderId(ActorNumber expectedSenderId)
    {
        _documentAsserter.HasValue("sender_MarketParticipant.mRID", expectedSenderId.Value);
        return this;
    }

    public IAssertRejectRequestMeasurementsDocument HasSenderRole(ActorRole expectedSenderRole)
    {
        _documentAsserter.HasValue("sender_MarketParticipant.marketRole.type", expectedSenderRole.Code);
        return this;
    }

    public IAssertRejectRequestMeasurementsDocument HasReceiverId(ActorNumber expectedReceiverId)
    {
        _documentAsserter.HasValue("receiver_MarketParticipant.mRID", expectedReceiverId.Value);
        return this;
    }

    public IAssertRejectRequestMeasurementsDocument HasReceiverRole(ActorRole expectedReceiverRole)
    {
        _documentAsserter.HasValue(
            "receiver_MarketParticipant.marketRole.type",
            expectedReceiverRole.Code);
        return this;
    }

    public IAssertRejectRequestMeasurementsDocument HasTimestamp(Instant expectedTimestamp)
    {
        _documentAsserter.HasValue("createdDateTime", expectedTimestamp.ToString());
        return this;
    }

    public IAssertRejectRequestMeasurementsDocument HasTransactionId(TransactionId expectedTransactionId)
    {
        _documentAsserter.HasValue("Series[1]/mRID", expectedTransactionId.Value);
        return this;
    }

    public IAssertRejectRequestMeasurementsDocument TransactionIdExists()
    {
        _documentAsserter.ElementExists("Series[1]/mRID");
        return this;
    }

    public IAssertRejectRequestMeasurementsDocument HasOriginalTransactionId(
        TransactionId expectedOriginalTransactionId)
    {
        _documentAsserter.HasValue(
            "Series[1]/originalTransactionIDReference_Series.mRID",
            expectedOriginalTransactionId.Value);
        return this;
    }

    public IAssertRejectRequestMeasurementsDocument HasMeteringPointId(
        MeteringPointId expectedMeteringPointId)
    {
        _documentAsserter.HasValue($"Series[1]/marketEvaluationPoint.mRID", expectedMeteringPointId.Value);
        return this;
    }

    public IAssertRejectRequestMeasurementsDocument HasSerieReasonCode(string expectedSerieReasonCode)
    {
        _documentAsserter.HasValue($"Series[1]/Reason[1]/code", expectedSerieReasonCode);
        return this;
    }

    public IAssertRejectRequestMeasurementsDocument HasSerieReasonMessage(string expectedSerieReasonMessage)
    {
        _documentAsserter.HasValue($"Series[1]/Reason[1]/text", expectedSerieReasonMessage);
        return this;
    }
}
