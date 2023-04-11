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
using Domain.Transactions.Aggregations;
using Tests.Infrastructure.OutgoingMessages.Asserts;

namespace Tests.Infrastructure.OutgoingMessages.AggregationResult;

public class AssertAggregationResultXmlDocument : IAssertAggregationResultDocument
{
    private readonly AssertXmlDocument _documentAsserter;

    public AssertAggregationResultXmlDocument(AssertXmlDocument documentAsserter)
    {
        _documentAsserter = documentAsserter;
        _documentAsserter.HasValue("type", "E31");
    }

    public IAssertAggregationResultDocument HasMessageId(string expectedMessageId)
    {
        _documentAsserter.HasValue("mRID", expectedMessageId);
        return this;
    }

    public IAssertAggregationResultDocument HasSenderId(string expectedSenderId)
    {
        _documentAsserter.HasValue("sender_MarketParticipant.mRID", expectedSenderId);
        return this;
    }

    public IAssertAggregationResultDocument HasReceiverId(string expectedReceiverId)
    {
        _documentAsserter.HasValue("receiver_MarketParticipant.mRID", expectedReceiverId);
        return this;
    }

    public IAssertAggregationResultDocument HasTimestamp(string expectedTimestamp)
    {
        _documentAsserter.HasValue("createdDateTime", expectedTimestamp);
        return this;
    }

    public IAssertAggregationResultDocument HasTransactionId(Guid expectedTransactionId)
    {
        _documentAsserter.HasValue($"Series[1]/mRID", expectedTransactionId.ToString());
        return this;
    }

    public IAssertAggregationResultDocument HasGridAreaCode(string expectedGridAreaCode)
    {
        _documentAsserter.HasValue("Series[1]/meteringGridArea_Domain.mRID", expectedGridAreaCode);
        return this;
    }

    public IAssertAggregationResultDocument HasBalanceResponsibleNumber(string expectedBalanceResponsibleNumber)
    {
        _documentAsserter.HasValue("Series[1]/balanceResponsibleParty_MarketParticipant.mRID", expectedBalanceResponsibleNumber);
        return this;
    }

    public IAssertAggregationResultDocument HasEnergySupplierNumber(string expectedEnergySupplierNumber)
    {
        _documentAsserter.HasValue("Series[1]/energySupplier_MarketParticipant.mRID", expectedEnergySupplierNumber);
        return this;
    }

    public IAssertAggregationResultDocument HasProductCode(string expectedProductCode)
    {
        _documentAsserter.HasValue("Series[1]/product", expectedProductCode);
        return this;
    }

    public IAssertAggregationResultDocument HasPeriod(Period expectedPeriod)
    {
        ArgumentNullException.ThrowIfNull(expectedPeriod);
        _documentAsserter
            .HasValue("Series[1]/Period/timeInterval/start", expectedPeriod.StartToString())
            .HasValue("Series[1]/Period/timeInterval/end", expectedPeriod.EndToString());
        return this;
    }

    public IAssertAggregationResultDocument HasPoint(int position, int quantity)
    {
        _documentAsserter
            .HasValue("Series[1]/Period/Point[1]/position", position.ToString(CultureInfo.InvariantCulture))
            .HasValue("Series[1]/Period/Point[1]/quantity", quantity.ToString(CultureInfo.InvariantCulture));
        return this;
    }

    public async Task<IAssertAggregationResultDocument> DocumentIsValidAsync()
    {
        await _documentAsserter.HasValidStructureAsync(DocumentType.AggregationResult).ConfigureAwait(false);
        return this;
    }

    public IAssertAggregationResultDocument SettlementMethodIsNotPresent()
    {
        _documentAsserter.IsNotPresent("Series[1]/marketEvaluationPoint.settlementMethod");
        return this;
    }

    public IAssertAggregationResultDocument EnergySupplierNumberIsNotPresent()
    {
        _documentAsserter.IsNotPresent("Series[1]/energySupplier_MarketParticipant.mRID");
        return this;
    }

    public IAssertAggregationResultDocument BalanceResponsibleNumberIsNotPresent()
    {
        _documentAsserter.IsNotPresent("Series[1]/balanceResponsibleParty_MarketParticipant.mRID");
        return this;
    }

    public IAssertAggregationResultDocument QuantityIsNotPresentForPosition(int position)
    {
        _documentAsserter.IsNotPresent($"Series[1]/Period/Point[{position}]/quantity");
        return this;
    }

    public IAssertAggregationResultDocument QualityIsNotPresentForPosition(int position)
    {
        _documentAsserter.IsNotPresent($"Series[1]/Period/Point[{position}]/quality");
        return this;
    }
}
