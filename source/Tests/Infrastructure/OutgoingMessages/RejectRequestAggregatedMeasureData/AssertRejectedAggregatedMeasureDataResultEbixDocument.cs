﻿// Copyright 2020 Energinet DataHub A/S
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

public class AssertRejectedAggregatedMeasureDataResultEbixDocument : IAssertRejectedAggregatedMeasureDataResultDocument
{
    private readonly AssertEbixDocument _documentAsserter;

    public AssertRejectedAggregatedMeasureDataResultEbixDocument(AssertEbixDocument documentAsserter)
    {
        _documentAsserter = documentAsserter;
        _documentAsserter.HasValue("HeaderEnergyDocument/DocumentType", "ERR");
    }

    public async Task<IAssertRejectedAggregatedMeasureDataResultDocument> DocumentIsValidAsync()
    {
        await _documentAsserter.HasValidStructureAsync(DocumentType.RejectRequestAggregatedMeasureData, "3").ConfigureAwait(false);
        return this;
    }

    public IAssertRejectedAggregatedMeasureDataResultDocument HasBusinessReason(BusinessReason businessReason)
    {
        _documentAsserter.HasValue("ProcessEnergyContext/EnergyBusinessProcess", EbixCode.Of(businessReason));
        return this;
    }

    public IAssertRejectedAggregatedMeasureDataResultDocument HasReasonCode(string reasonCode)
    {
        _documentAsserter.HasValue("PayloadResponseEvent[1]/StatusType", EbixCode.Of(ReasonCode.From(reasonCode)));
        return this;
    }

    public IAssertRejectedAggregatedMeasureDataResultDocument HasMessageId(string expectedMessageId)
    {
        _documentAsserter.HasValue("HeaderEnergyDocument/Identification", expectedMessageId);
        return this;
    }

    public IAssertRejectedAggregatedMeasureDataResultDocument HasSenderId(string expectedSenderId)
    {
        _documentAsserter.HasValue("HeaderEnergyDocument/SenderEnergyParty/Identification", expectedSenderId);
        return this;
    }

    public IAssertRejectedAggregatedMeasureDataResultDocument HasReceiverId(string expectedReceiverId)
    {
        _documentAsserter.HasValue("HeaderEnergyDocument/RecipientEnergyParty/Identification", expectedReceiverId);
        return this;
    }

    public IAssertRejectedAggregatedMeasureDataResultDocument HasTimestamp(Instant expectedTimestamp)
    {
        _documentAsserter.HasValue("HeaderEnergyDocument/Creation", expectedTimestamp.ToString());
        return this;
    }

    public IAssertRejectedAggregatedMeasureDataResultDocument HasTransactionId(Guid expectedTransactionId)
    {
        _documentAsserter.HasValue($"PayloadResponseEvent[1]/Identification", expectedTransactionId.ToString("N"));
        return this;
    }

    public IAssertRejectedAggregatedMeasureDataResultDocument HasOriginalTransactionId(string expectedOriginalTransactionId)
    {
        _documentAsserter.HasValue("PayloadResponseEvent[1]/OriginalBusinessDocument", expectedOriginalTransactionId);
        return this;
    }

    public IAssertRejectedAggregatedMeasureDataResultDocument HasSerieReasonCode(string expectedSerieReasonCode)
    {
        _documentAsserter.HasValue($"PayloadResponseEvent[1]/ResponseReasonType", expectedSerieReasonCode);
        return this;
    }

    public IAssertRejectedAggregatedMeasureDataResultDocument HasSerieReasonMessage(string expectedSerieReasonMessage)
    {
        //In ebIX we don't have a field for text information meaning this method should always assert true
        return this;
    }
}
