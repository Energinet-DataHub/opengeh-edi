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
using NodaTime;
using OutgoingMessages.Application.Tests.MarketDocuments.Asserts;

namespace OutgoingMessages.Application.Tests.MarketDocuments.RejectRequestAggregatedMeasureData;

public class AssertRejectRequestAggregatedMeasureDataXmlDocument : IAssertRejectRequestAggregatedMeasureDataDocument
{
    private readonly AssertXmlDocument _documentAsserter;

    public AssertRejectRequestAggregatedMeasureDataXmlDocument(AssertXmlDocument documentAsserter)
    {
        _documentAsserter = documentAsserter;
        _documentAsserter.HasValue("type", "ERR");
    }

    public async Task<IAssertRejectRequestAggregatedMeasureDataDocument> DocumentIsValidAsync()
    {
        await _documentAsserter.HasValidStructureAsync(DocumentType.RejectRequestAggregatedMeasureData).ConfigureAwait(false);
        return this;
    }

    public IAssertRejectRequestAggregatedMeasureDataDocument HasBusinessReason(BusinessReason businessReason)
    {
        ArgumentNullException.ThrowIfNull(businessReason);
        _documentAsserter.HasValue("process.processType", businessReason.Code);
        return this;
    }

    public IAssertRejectRequestAggregatedMeasureDataDocument HasReasonCode(string reasonCode)
    {
        _documentAsserter.HasValue("reason.code", reasonCode);
        return this;
    }

    public IAssertRejectRequestAggregatedMeasureDataDocument HasMessageId(string expectedMessageId)
    {
        _documentAsserter.HasValue("mRID", expectedMessageId);
        return this;
    }

    public IAssertRejectRequestAggregatedMeasureDataDocument HasSenderId(string expectedSenderId)
    {
        _documentAsserter.HasValue("sender_MarketParticipant.mRID", expectedSenderId);
        return this;
    }

    public IAssertRejectRequestAggregatedMeasureDataDocument HasReceiverId(string expectedReceiverId)
    {
        _documentAsserter.HasValue("receiver_MarketParticipant.mRID", expectedReceiverId);
        return this;
    }

    public IAssertRejectRequestAggregatedMeasureDataDocument HasTimestamp(Instant expectedTimestamp)
    {
        _documentAsserter.HasValue("createdDateTime", expectedTimestamp.ToString());
        return this;
    }

    public IAssertRejectRequestAggregatedMeasureDataDocument HasTransactionId(Guid expectedTransactionId)
    {
        _documentAsserter.HasValue($"Series[1]/mRID", expectedTransactionId.ToString());
        return this;
    }

    public IAssertRejectRequestAggregatedMeasureDataDocument HasOriginalTransactionId(string expectedOriginalTransactionId)
    {
        _documentAsserter.HasValue($"Series[1]/originalTransactionIDReference_Series.mRID", expectedOriginalTransactionId);
        return this;
    }

    public IAssertRejectRequestAggregatedMeasureDataDocument HasSerieReasonCode(string expectedSerieReasonCode)
    {
        _documentAsserter.HasValue($"Series[1]/Reason[1]/code", expectedSerieReasonCode);
        return this;
    }

    public IAssertRejectRequestAggregatedMeasureDataDocument HasSerieReasonMessage(string expectedSerieReasonMessage)
    {
        _documentAsserter.HasValue($"Series[1]/Reason[1]/text", expectedSerieReasonMessage);
        return this;
    }
}
