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
using System.Globalization;
using System.Threading.Tasks;
using DocumentValidation;
using Domain.OutgoingMessages;
using Domain.Transactions.Aggregations;
using Infrastructure.OutgoingMessages.Common;
using Tests.Infrastructure.OutgoingMessages.AggregationResult;
using Tests.Infrastructure.OutgoingMessages.Asserts;

namespace Tests.Infrastructure.OutgoingMessages.RejectRequestAggregatedMeasureData;

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
        await _documentAsserter.HasValidStructureAsync(DocumentType.AggregationResult).ConfigureAwait(false);
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

    public IAssertRejectedAggregatedMeasureDataResultDocument HasTimestamp(string expectedTimestamp)
    {
        _documentAsserter.HasValue("createdDateTime", expectedTimestamp);
        return this;
    }

    public IAssertRejectedAggregatedMeasureDataResultDocument HasTransactionId(Guid expectedTransactionId)
    {
        _documentAsserter.HasValue($"Series[1]/mRID", expectedTransactionId.ToString());
        return this;
    }

    public IAssertRejectedAggregatedMeasureDataResultDocument HasOriginalTransactionId(Guid expectedOriginalTransactionId)
    {
        _documentAsserter.HasValue($"Series[1]/originalTransactionIDReference_Series.mRID", expectedOriginalTransactionId.ToString());
        return this;
    }

    public IAssertRejectedAggregatedMeasureDataResultDocument HasSeriesReasonCode(string expectedSeriesReasonCode)
    {
        _documentAsserter.HasValue($"Series[1]/Reason[1]/code", expectedSeriesReasonCode);
        return this;
    }

    public IAssertRejectedAggregatedMeasureDataResultDocument HasSeriesReasonMessage(string expectedSeriesReasonMessage)
    {
        _documentAsserter.HasValue($"Series[1]/Reason[1]/text", expectedSeriesReasonMessage);
        return this;
    }
}
