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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats.Ebix;
using Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.Asserts;
using NodaTime;

namespace Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.RejectRequestWholesaleSettlement;

public class AssertRejectRequestWholesaleSettlementEbixDocument : IAssertRejectRequestWholesaleSettlementDocument
{
    private readonly AssertEbixDocument _documentAsserter;
    private readonly bool _skipIdentificationLengthValidation;

    public AssertRejectRequestWholesaleSettlementEbixDocument(AssertEbixDocument documentAsserter, bool skipIdentificationLengthValidation = false)
    {
        _documentAsserter = documentAsserter;
        _skipIdentificationLengthValidation = skipIdentificationLengthValidation;
        _documentAsserter.HasValue("HeaderEnergyDocument/DocumentType", "ERR");
    }

    public async Task<IAssertRejectRequestWholesaleSettlementDocument> DocumentIsValidAsync()
    {
        await _documentAsserter.HasValidStructureAsync(DocumentType.RejectRequestWholesaleSettlement, "3", _skipIdentificationLengthValidation)
            .ConfigureAwait(false);
        return this;
    }

    public IAssertRejectRequestWholesaleSettlementDocument HasBusinessReason(BusinessReason businessReason)
    {
        _documentAsserter.HasValue("ProcessEnergyContext/EnergyBusinessProcess", EbixCode.Of(businessReason));
        return this;
    }

    public IAssertRejectRequestWholesaleSettlementDocument HasReasonCode(string reasonCode)
    {
        _documentAsserter.HasValue("PayloadChargeEvent[1]/StatusType", EbixCode.Of(ReasonCode.FromCode(reasonCode)));
        return this;
    }

    public IAssertRejectRequestWholesaleSettlementDocument HasMessageId(string expectedMessageId)
    {
        _documentAsserter.HasValue("HeaderEnergyDocument/Identification", expectedMessageId);
        return this;
    }

    public IAssertRejectRequestWholesaleSettlementDocument MessageIdExists()
    {
        _documentAsserter.ElementExists("HeaderEnergyDocument/Identification");
        return this;
    }

    public IAssertRejectRequestWholesaleSettlementDocument HasSenderId(string expectedSenderId)
    {
        _documentAsserter.HasValue("HeaderEnergyDocument/SenderEnergyParty/Identification", expectedSenderId);
        return this;
    }

    public IAssertRejectRequestWholesaleSettlementDocument HasSenderRole(ActorRole role)
    {
        // Sender role for RejectRequestWholesaleSettlement doesn't exist in ebIX, so do nothing
        return this;
    }

    public IAssertRejectRequestWholesaleSettlementDocument HasReceiverId(string expectedReceiverId)
    {
        _documentAsserter.HasValue("HeaderEnergyDocument/RecipientEnergyParty/Identification", expectedReceiverId);
        return this;
    }

    public IAssertRejectRequestWholesaleSettlementDocument HasReceiverRole(ActorRole role)
    {
        _documentAsserter.HasValue("ProcessEnergyContext/EnergyBusinessProcessRole", EbixCode.Of(role));
        return this;
    }

    public IAssertRejectRequestWholesaleSettlementDocument HasTimestamp(Instant expectedTimestamp)
    {
        _documentAsserter.HasValue("HeaderEnergyDocument/Creation", expectedTimestamp.ToString());
        return this;
    }

    public IAssertRejectRequestWholesaleSettlementDocument HasTransactionId(TransactionId expectedTransactionId)
    {
        ArgumentNullException.ThrowIfNull(expectedTransactionId);
        _documentAsserter.HasValue("PayloadChargeEvent[1]/Identification", expectedTransactionId.Value);
        return this;
    }

    public IAssertRejectRequestWholesaleSettlementDocument TransactionIdExists()
    {
        _documentAsserter.ElementExists("PayloadChargeEvent[1]/Identification");
        return this;
    }

    public IAssertRejectRequestWholesaleSettlementDocument HasOriginalTransactionId(
        TransactionId expectedOriginalTransactionId)
    {
        ArgumentNullException.ThrowIfNull(expectedOriginalTransactionId);
        _documentAsserter.HasValue("PayloadChargeEvent[1]/OriginalBusinessDocument", expectedOriginalTransactionId.Value);
        return this;
    }

    public IAssertRejectRequestWholesaleSettlementDocument HasSerieReasonCode(string expectedSerieReasonCode)
    {
        _documentAsserter.HasValue("PayloadChargeEvent[1]/ResponseReasonType", expectedSerieReasonCode);
        return this;
    }

    public IAssertRejectRequestWholesaleSettlementDocument HasSerieReasonMessage(string expectedSerieReasonMessage)
    {
        //In ebIX we don't have a field for text information meaning this method should always assert true
        return this;
    }
}
