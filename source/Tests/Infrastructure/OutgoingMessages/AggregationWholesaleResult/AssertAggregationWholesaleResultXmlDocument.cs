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
using Energinet.DataHub.EDI.Domain.OutgoingMessages;
using Energinet.DataHub.EDI.Domain.Transactions.Aggregations;
using Energinet.DataHub.EDI.Infrastructure.DocumentValidation;
using Energinet.DataHub.EDI.Infrastructure.OutgoingMessages.Common;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.Asserts;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.AggregationWholesaleResult;

public class AssertAggregationWholesaleResultXmlDocument : IAssertAggregationWholesaleResultDocument
{
    private readonly AssertXmlDocument _documentAsserter;

    public AssertAggregationWholesaleResultXmlDocument(AssertXmlDocument documentAsserter)
    {
        _documentAsserter = documentAsserter;
        _documentAsserter.HasValue("type", "E31");
    }

    public IAssertAggregationWholesaleResultDocument HasMessageId(string expectedMessageId)
    {
        _documentAsserter.HasValue("mRID", expectedMessageId);
        return this;
    }

    public IAssertAggregationWholesaleResultDocument HasSenderId(string expectedSenderId)
    {
        _documentAsserter.HasValue("sender_MarketParticipant.mRID", expectedSenderId);
        return this;
    }

    public IAssertAggregationWholesaleResultDocument HasReceiverId(string expectedReceiverId)
    {
        _documentAsserter.HasValue("receiver_MarketParticipant.mRID", expectedReceiverId);
        return this;
    }

    public IAssertAggregationWholesaleResultDocument HasTimestamp(string expectedTimestamp)
    {
        _documentAsserter.HasValue("createdDateTime", expectedTimestamp);
        return this;
    }

    public IAssertAggregationWholesaleResultDocument HasTransactionId(Guid expectedTransactionId)
    {
        _documentAsserter.HasValue($"Series[1]/mRID", expectedTransactionId.ToString());
        return this;
    }

    public IAssertAggregationWholesaleResultDocument HasGridAreaCode(string expectedGridAreaCode)
    {
        _documentAsserter.HasValue("Series[1]/meteringGridArea_Domain.mRID", expectedGridAreaCode);
        return this;
    }

    public IAssertAggregationWholesaleResultDocument HasBalanceResponsibleNumber(string expectedBalanceResponsibleNumber)
    {
        _documentAsserter.HasValue("Series[1]/balanceResponsibleParty_MarketParticipant.mRID", expectedBalanceResponsibleNumber);
        return this;
    }

    public IAssertAggregationWholesaleResultDocument HasEnergySupplierNumber(string expectedEnergySupplierNumber)
    {
        _documentAsserter.HasValue("Series[1]/energySupplier_MarketParticipant.mRID", expectedEnergySupplierNumber);
        return this;
    }

    public IAssertAggregationWholesaleResultDocument HasProductCode(string expectedProductCode)
    {
        _documentAsserter.HasValue("Series[1]/product", expectedProductCode);
        return this;
    }

    public IAssertAggregationWholesaleResultDocument HasPeriod(Period expectedPeriod)
    {
        ArgumentNullException.ThrowIfNull(expectedPeriod);
        _documentAsserter
            .HasValue("Series[1]/Period/timeInterval/start", expectedPeriod.StartToString())
            .HasValue("Series[1]/Period/timeInterval/end", expectedPeriod.EndToString());
        return this;
    }

    public IAssertAggregationWholesaleResultDocument HasPoint(int position, int quantity)
    {
        _documentAsserter
            .HasValue("Series[1]/Period/Point[1]/position", position.ToString(CultureInfo.InvariantCulture))
            .HasValue("Series[1]/Period/Point[1]/quantity", quantity.ToString(CultureInfo.InvariantCulture));
        return this;
    }

    public async Task<IAssertAggregationWholesaleResultDocument> DocumentIsValidAsync()
    {
        await _documentAsserter.HasValidStructureAsync(DocumentType.AggregationResult).ConfigureAwait(false);
        return this;
    }

    public IAssertAggregationWholesaleResultDocument SettlementMethodIsNotPresent()
    {
        _documentAsserter.IsNotPresent("Series[1]/marketEvaluationPoint.settlementMethod");
        return this;
    }

    public IAssertAggregationWholesaleResultDocument EnergySupplierNumberIsNotPresent()
    {
        _documentAsserter.IsNotPresent("Series[1]/energySupplier_MarketParticipant.mRID");
        return this;
    }

    public IAssertAggregationWholesaleResultDocument BalanceResponsibleNumberIsNotPresent()
    {
        _documentAsserter.IsNotPresent("Series[1]/balanceResponsibleParty_MarketParticipant.mRID");
        return this;
    }

    public IAssertAggregationWholesaleResultDocument QuantityIsNotPresentForPosition(int position)
    {
        _documentAsserter.IsNotPresent($"Series[1]/Period/Point[{position}]/quantity");
        return this;
    }

    public IAssertAggregationWholesaleResultDocument QualityIsNotPresentForPosition(int position)
    {
        _documentAsserter.IsNotPresent($"Series[1]/Period/Point[{position}]/quality");
        return this;
    }

    public IAssertAggregationWholesaleResultDocument HasBusinessReason(BusinessReason businessReason)
    {
        _documentAsserter.HasValue("process.processType", CimCode.Of(businessReason));
        return this;
    }

    public IAssertAggregationWholesaleResultDocument HasSettlementVersion(SettlementVersion settlementVersion)
    {
        _documentAsserter.HasValue("Series[1]/settlement_Series.version", CimCode.Of(settlementVersion));
        return this;
    }

    public IAssertAggregationWholesaleResultDocument SettlementVersionIsNotPresent()
    {
        _documentAsserter.IsNotPresent("Series[1]/settlement_Series.version");
        return this;
    }

    public IAssertAggregationWholesaleResultDocument HasOriginalTransactionIdReference(string originalTransactionIdReference)
    {
        _documentAsserter.HasValue("Series[1]/originalTransactionIDReference_Series.mRID", originalTransactionIdReference);
        return this;
    }

    public IAssertAggregationWholesaleResultDocument HasSettlementMethod(SettlementType settlementMethod)
    {
        _documentAsserter.HasValue("Series[1]/marketEvaluationPoint.settlementMethod", CimCode.Of(settlementMethod));
        return this;
    }
}
