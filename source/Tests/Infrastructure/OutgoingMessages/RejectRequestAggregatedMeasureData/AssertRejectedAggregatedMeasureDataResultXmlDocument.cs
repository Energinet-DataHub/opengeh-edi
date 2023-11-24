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
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.Common;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.MarketDocuments;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.Asserts;
using NodaTime;
using DocumentType = IncomingMessages.Infrastructure.DocumentValidation.DocumentType;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.RejectRequestAggregatedMeasureData;

public class AssertRejectedAggregatedMeasureDataResultXmlDocument : IAssertRejectedAggregatedMeasureDataResultDocument
{
    private readonly AssertXmlDocument _documentAsserter;

    public AssertRejectedAggregatedMeasureDataResultXmlDocument(AssertXmlDocument documentAsserter)
    {
        _documentAsserter = documentAsserter;
        _documentAsserter.HasValue("type", "ERR");
    }

    public async Task<IAssertRejectedAggregatedMeasureDataResultDocument> DocumentIsValidAsync()
    {
        await _documentAsserter.HasValidStructureAsync(DocumentType.RejectRequestAggregatedMeasureData).ConfigureAwait(false);
        return this;
    }

    public IAssertRejectedAggregatedMeasureDataResultDocument HasBusinessReason(BusinessReason businessReason)
    {
        _documentAsserter.HasValue("process.processType", CimCode.Of(businessReason));
        return this;
    }

    public IAssertRejectedAggregatedMeasureDataResultDocument HasReasonCode(string reasonCode)
    {
        _documentAsserter.HasValue("reason.code", reasonCode);
        return this;
    }

    public IAssertRejectedAggregatedMeasureDataResultDocument HasMessageId(string expectedMessageId)
    {
        _documentAsserter.HasValue("mRID", expectedMessageId);
        return this;
    }

    public IAssertRejectedAggregatedMeasureDataResultDocument HasSenderId(string expectedSenderId)
    {
        _documentAsserter.HasValue("sender_MarketParticipant.mRID", expectedSenderId);
        return this;
    }

    public IAssertRejectedAggregatedMeasureDataResultDocument HasReceiverId(string expectedReceiverId)
    {
        _documentAsserter.HasValue("receiver_MarketParticipant.mRID", expectedReceiverId);
        return this;
    }

    public IAssertRejectedAggregatedMeasureDataResultDocument HasTimestamp(Instant expectedTimestamp)
    {
        _documentAsserter.HasValue("createdDateTime", expectedTimestamp.ToString());
        return this;
    }

    public IAssertRejectedAggregatedMeasureDataResultDocument HasTransactionId(Guid expectedTransactionId)
    {
        _documentAsserter.HasValue($"Series[1]/mRID", expectedTransactionId.ToString());
        return this;
    }

    public IAssertRejectedAggregatedMeasureDataResultDocument HasOriginalTransactionId(string expectedOriginalTransactionId)
    {
        _documentAsserter.HasValue($"Series[1]/originalTransactionIDReference_Series.mRID", expectedOriginalTransactionId);
        return this;
    }

    public IAssertRejectedAggregatedMeasureDataResultDocument HasSerieReasonCode(string expectedSerieReasonCode)
    {
        _documentAsserter.HasValue($"Series[1]/Reason[1]/code", expectedSerieReasonCode);
        return this;
    }

    public IAssertRejectedAggregatedMeasureDataResultDocument HasSerieReasonMessage(string expectedSerieReasonMessage)
    {
        _documentAsserter.HasValue($"Series[1]/Reason[1]/text", expectedSerieReasonMessage);
        return this;
    }
}
