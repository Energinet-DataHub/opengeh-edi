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
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats.CIM;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.Asserts;
using NodaTime;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.RejectRequestWholesaleSettlement;

public class AssertRejectRequestWholesaleSettlementXmlDocument : IAssertRejectRequestWholesaleSettlementDocument
{
    private readonly AssertXmlDocument _documentAsserter;

    public AssertRejectRequestWholesaleSettlementXmlDocument(AssertXmlDocument documentAsserter)
    {
        _documentAsserter = documentAsserter;
        _documentAsserter.HasValue("type", "ERR");
    }

    public async Task<IAssertRejectRequestWholesaleSettlementDocument> DocumentIsValidAsync()
    {
        await _documentAsserter.HasValidStructureAsync(DocumentType.RejectRequestWholesaleSettlement)
            .ConfigureAwait(false);
        return this;
    }

    public IAssertRejectRequestWholesaleSettlementDocument HasBusinessReason(BusinessReason businessReason)
    {
        ArgumentNullException.ThrowIfNull(businessReason);
        _documentAsserter.HasValue("process.processType", businessReason.Code);
        return this;
    }

    public IAssertRejectRequestWholesaleSettlementDocument HasReasonCode(string reasonCode)
    {
        _documentAsserter.HasValue("reason.code", reasonCode);
        return this;
    }

    public IAssertRejectRequestWholesaleSettlementDocument HasMessageId(string expectedMessageId)
    {
        _documentAsserter.HasValue("mRID", expectedMessageId);
        return this;
    }

    public IAssertRejectRequestWholesaleSettlementDocument MessageIdExists()
    {
        _documentAsserter.ElementExists("mRID");
        return this;
    }

    public IAssertRejectRequestWholesaleSettlementDocument HasSenderId(string expectedSenderId)
    {
        _documentAsserter.HasValue("sender_MarketParticipant.mRID", expectedSenderId);
        return this;
    }

    public IAssertRejectRequestWholesaleSettlementDocument HasSenderRole(ActorRole role)
    {
        ArgumentNullException.ThrowIfNull(role);
        _documentAsserter.HasValue("sender_MarketParticipant.marketRole.type", role.Code);
        return this;
    }

    public IAssertRejectRequestWholesaleSettlementDocument HasReceiverId(string expectedReceiverId)
    {
        _documentAsserter.HasValue("receiver_MarketParticipant.mRID", expectedReceiverId);
        return this;
    }

    public IAssertRejectRequestWholesaleSettlementDocument HasReceiverRole(ActorRole role)
    {
        ArgumentNullException.ThrowIfNull(role);
        _documentAsserter.HasValue("receiver_MarketParticipant.marketRole.type", role.Code);
        return this;
    }

    public IAssertRejectRequestWholesaleSettlementDocument HasTimestamp(Instant expectedTimestamp)
    {
        _documentAsserter.HasValue("createdDateTime", expectedTimestamp.ToString());
        return this;
    }

    public IAssertRejectRequestWholesaleSettlementDocument HasTransactionId(Guid expectedTransactionId)
    {
        _documentAsserter.HasValue("Series[1]/mRID", expectedTransactionId.ToString());
        return this;
    }

    public IAssertRejectRequestWholesaleSettlementDocument TransactionIdExists()
    {
        _documentAsserter.ElementExists("Series[1]/mRID");
        return this;
    }

    public IAssertRejectRequestWholesaleSettlementDocument HasOriginalTransactionId(
        string expectedOriginalTransactionId)
    {
        _documentAsserter.HasValue(
            "Series[1]/originalTransactionIDReference_Series.mRID",
            expectedOriginalTransactionId);
        return this;
    }

    public IAssertRejectRequestWholesaleSettlementDocument HasSerieReasonCode(string expectedSerieReasonCode)
    {
        _documentAsserter.HasValue("Series[1]/Reason[1]/code", expectedSerieReasonCode);
        return this;
    }

    public IAssertRejectRequestWholesaleSettlementDocument HasSerieReasonMessage(string expectedSerieReasonMessage)
    {
        _documentAsserter.HasValue("Series[1]/Reason[1]/text", expectedSerieReasonMessage);
        return this;
    }
}
