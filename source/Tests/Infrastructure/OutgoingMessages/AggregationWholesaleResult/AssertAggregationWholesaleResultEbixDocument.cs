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

public class AssertAggregationWholesaleResultEbixDocument : IAssertAggregationWholesaleResultDocument
{
    private readonly AssertEbixDocument _documentAsserter;

    public AssertAggregationWholesaleResultEbixDocument(AssertEbixDocument documentAsserter)
    {
        _documentAsserter = documentAsserter;
        _documentAsserter.HasValue("HeaderEnergyDocument/DocumentType", "E31");
    }

    public IAssertAggregationWholesaleResultDocument HasMessageId(string expectedMessageId)
    {
        _documentAsserter.HasValue("HeaderEnergyDocument/Identification", expectedMessageId);
        return this;
    }

    public IAssertAggregationWholesaleResultDocument HasSenderId(string expectedSenderId)
    {
        _documentAsserter.HasValue("HeaderEnergyDocument/SenderEnergyParty/Identification", expectedSenderId);
        return this;
    }

    public IAssertAggregationWholesaleResultDocument HasReceiverId(string expectedReceiverId)
    {
        _documentAsserter.HasValue("HeaderEnergyDocument/RecipientEnergyParty/Identification", expectedReceiverId);
        return this;
    }

    public IAssertAggregationWholesaleResultDocument HasTimestamp(string expectedTimestamp)
    {
        _documentAsserter.HasValue("HeaderEnergyDocument/Creation", expectedTimestamp);
        return this;
    }

    public IAssertAggregationWholesaleResultDocument HasTransactionId(Guid expectedTransactionId)
    {
        _documentAsserter.HasValue($"PayloadEnergyTimeSeries[1]/Identification", expectedTransactionId.ToString("N"));
        return this;
    }

    public IAssertAggregationWholesaleResultDocument HasGridAreaCode(string expectedGridAreaCode)
    {
        _documentAsserter.HasValue("PayloadEnergyTimeSeries[1]/MeteringGridAreaUsedDomainLocation/Identification", expectedGridAreaCode);
        return this;
    }

    public IAssertAggregationWholesaleResultDocument HasBalanceResponsibleNumber(string expectedBalanceResponsibleNumber)
    {
        _documentAsserter.HasValue("PayloadEnergyTimeSeries[1]/BalanceResponsibleEnergyParty/Identification", expectedBalanceResponsibleNumber);
        return this;
    }

    public IAssertAggregationWholesaleResultDocument HasEnergySupplierNumber(string expectedEnergySupplierNumber)
    {
        _documentAsserter.HasValue("PayloadEnergyTimeSeries[1]/BalanceSupplierEnergyParty/Identification", expectedEnergySupplierNumber);
        return this;
    }

    public IAssertAggregationWholesaleResultDocument HasProductCode(string expectedProductCode)
    {
        _documentAsserter.HasValue("PayloadEnergyTimeSeries[1]/IncludedProductCharacteristic/Identification", expectedProductCode);
        return this;
    }

    public IAssertAggregationWholesaleResultDocument HasPeriod(Period expectedPeriod)
    {
        ArgumentNullException.ThrowIfNull(expectedPeriod);
        _documentAsserter
            .HasValue("PayloadEnergyTimeSeries[1]/ObservationTimeSeriesPeriod/Start", expectedPeriod.StartToEbixString())
            .HasValue("PayloadEnergyTimeSeries[1]/ObservationTimeSeriesPeriod/End", expectedPeriod.EndToEbixString());
        return this;
    }

    public IAssertAggregationWholesaleResultDocument HasPoint(int position, int quantity)
    {
        _documentAsserter
            .HasValue("PayloadEnergyTimeSeries[1]/IntervalEnergyObservation[1]/Position", position.ToString(CultureInfo.InvariantCulture))
            .HasValue("PayloadEnergyTimeSeries[1]/IntervalEnergyObservation[1]/EnergyQuantity", quantity.ToString(CultureInfo.InvariantCulture));
        return this;
    }

    public async Task<IAssertAggregationWholesaleResultDocument> DocumentIsValidAsync()
    {
        await _documentAsserter.HasValidStructureAsync(DocumentType.AggregationResult, "3").ConfigureAwait(false);
        return this;
    }

    public IAssertAggregationWholesaleResultDocument SettlementMethodIsNotPresent()
    {
        _documentAsserter.IsNotPresent("PayloadEnergyTimeSeries[1]/DetailMeasurementMeteringPointCharacteristic/SettlementMethod");
        return this;
    }

    public IAssertAggregationWholesaleResultDocument EnergySupplierNumberIsNotPresent()
    {
        _documentAsserter.IsNotPresent("PayloadEnergyTimeSeries[1]/BalanceSupplierEnergyParty/Identification");
        return this;
    }

    public IAssertAggregationWholesaleResultDocument BalanceResponsibleNumberIsNotPresent()
    {
        _documentAsserter.IsNotPresent("PayloadEnergyTimeSeries[1]/BalanceResponsibleEnergyParty/Identification");
        return this;
    }

    public IAssertAggregationWholesaleResultDocument QuantityIsNotPresentForPosition(int position)
    {
        _documentAsserter.IsNotPresent($"PayloadEnergyTimeSeries[1]/IntervalEnergyObservation[{position}]/EnergyQuantity");
        return this;
    }

    public IAssertAggregationWholesaleResultDocument QualityIsNotPresentForPosition(int position)
    {
        _documentAsserter.IsNotPresent($"PayloadEnergyTimeSeries[1]/IntervalEnergyObservation[{position}]/QuantityQuality");
        _documentAsserter.HasValue($"PayloadEnergyTimeSeries[1]/IntervalEnergyObservation[{position}]/QuantityMissing", "true");
        return this;
    }

    public IAssertAggregationWholesaleResultDocument HasBusinessReason(BusinessReason businessReason)
    {
        _documentAsserter.HasValue("ProcessEnergyContext/EnergyBusinessProcess", EbixCode.Of(businessReason));
        return this;
    }

    public IAssertAggregationWholesaleResultDocument HasSettlementVersion(SettlementVersion settlementVersion)
    {
        _documentAsserter.HasValue("ProcessEnergyContext/ProcessVariant", EbixCode.Of(settlementVersion));
        return this;
    }

    public IAssertAggregationWholesaleResultDocument SettlementVersionIsNotPresent()
    {
        _documentAsserter.IsNotPresent("ProcessEnergyContext/ProcessVariant");
        return this;
    }

    public IAssertAggregationWholesaleResultDocument HasOriginalTransactionIdReference(string originalTransactionIdReference)
    {
        _documentAsserter.HasValue("PayloadEnergyTimeSeries[1]/OriginalBusinessDocument", originalTransactionIdReference);
        return this;
    }

    public IAssertAggregationWholesaleResultDocument HasSettlementMethod(SettlementType settlementMethod)
    {
        _documentAsserter.HasValue("PayloadEnergyTimeSeries[1]/DetailMeasurementMeteringPointCharacteristic/SettlementMethod", EbixCode.Of(settlementMethod));
        return this;
    }
}
