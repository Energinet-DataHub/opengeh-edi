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
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats.Ebix;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.Asserts;
using NodaTime;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.RejectRequestAggregatedMeasureData;

public class AssertRejectRequestAggregatedMeasureDataEbixDocument : IAssertRejectRequestAggregatedMeasureDataDocument
{
    private readonly AssertEbixDocument _documentAsserter;
    private readonly bool _skipMaxLengthValidation;

    public AssertRejectRequestAggregatedMeasureDataEbixDocument(AssertEbixDocument documentAsserter, bool skipMaxLengthValidation = false)
    {
        _documentAsserter = documentAsserter;
        _skipMaxLengthValidation = skipMaxLengthValidation;
        _documentAsserter.HasValue("HeaderEnergyDocument/DocumentType", "ERR");
    }

    public async Task<IAssertRejectRequestAggregatedMeasureDataDocument> DocumentIsValidAsync()
    {
        await _documentAsserter.HasValidStructureAsync(DocumentType.RejectRequestAggregatedMeasureData, "3", _skipMaxLengthValidation).ConfigureAwait(false);
        return this;
    }

    public IAssertRejectRequestAggregatedMeasureDataDocument HasBusinessReason(BusinessReason businessReason)
    {
        _documentAsserter.HasValue("ProcessEnergyContext/EnergyBusinessProcess", EbixCode.Of(businessReason));
        return this;
    }

    public IAssertRejectRequestAggregatedMeasureDataDocument HasReasonCode(string reasonCode)
    {
        _documentAsserter.HasValue("PayloadResponseEvent[1]/StatusType", EbixCode.Of(ReasonCode.FromCode(reasonCode)));
        return this;
    }

    public IAssertRejectRequestAggregatedMeasureDataDocument HasMessageId(string expectedMessageId)
    {
        _documentAsserter.HasValue("HeaderEnergyDocument/Identification", expectedMessageId);
        return this;
    }

    public IAssertRejectRequestAggregatedMeasureDataDocument MessageIdExists()
    {
        _documentAsserter.ElementExists("HeaderEnergyDocument/Identification");
        return this;
    }

    public IAssertRejectRequestAggregatedMeasureDataDocument HasSenderId(string expectedSenderId)
    {
        _documentAsserter.HasValue("HeaderEnergyDocument/SenderEnergyParty/Identification", expectedSenderId);
        return this;
    }

    public IAssertRejectRequestAggregatedMeasureDataDocument HasReceiverId(string expectedReceiverId)
    {
        _documentAsserter.HasValue("HeaderEnergyDocument/RecipientEnergyParty/Identification", expectedReceiverId);
        return this;
    }

    public IAssertRejectRequestAggregatedMeasureDataDocument HasTimestamp(Instant expectedTimestamp)
    {
        _documentAsserter.HasValue("HeaderEnergyDocument/Creation", expectedTimestamp.ToString());
        return this;
    }

    public IAssertRejectRequestAggregatedMeasureDataDocument HasTransactionId(Guid expectedTransactionId)
    {
        _documentAsserter.HasValue($"PayloadResponseEvent[1]/Identification", expectedTransactionId.ToString("N"));
        return this;
    }

    public IAssertRejectRequestAggregatedMeasureDataDocument TransactionIdExists()
    {
        _documentAsserter.ElementExists("PayloadResponseEvent[1]/Identification");
        return this;
    }

    public IAssertRejectRequestAggregatedMeasureDataDocument HasOriginalTransactionId(string expectedOriginalTransactionId)
    {
        _documentAsserter.HasValue("PayloadResponseEvent[1]/OriginalBusinessDocument", expectedOriginalTransactionId);
        return this;
    }

    public IAssertRejectRequestAggregatedMeasureDataDocument HasSerieReasonCode(string expectedSerieReasonCode)
    {
        _documentAsserter.HasValue($"PayloadResponseEvent[1]/ResponseReasonType", expectedSerieReasonCode);
        return this;
    }

    public IAssertRejectRequestAggregatedMeasureDataDocument HasSerieReasonMessage(string expectedSerieReasonMessage)
    {
        //In ebIX we don't have a field for text information meaning this method should always assert true
        return this;
    }
}
