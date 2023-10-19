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

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.AggregationResult;

public class AssertAggregationResultEbixDocument : IAssertAggregationResultDocument
{
    private readonly AssertEbixDocument _documentAsserter;

    public AssertAggregationResultEbixDocument(AssertEbixDocument documentAsserter)
    {
        _documentAsserter = documentAsserter;
        _documentAsserter.HasValue("HeaderEnergyDocument/DocumentType", "E31");
    }

    public IAssertAggregationResultDocument HasMessageId(string expectedMessageId)
    {
        _documentAsserter.HasValue("HeaderEnergyDocument/Identification", expectedMessageId);
        return this;
    }

    public IAssertAggregationResultDocument HasSenderId(string expectedSenderId)
    {
        _documentAsserter.HasValue("HeaderEnergyDocument/SenderEnergyParty/Identification", expectedSenderId);
        return this;
    }

    public IAssertAggregationResultDocument HasReceiverId(string expectedReceiverId)
    {
        _documentAsserter.HasValue("HeaderEnergyDocument/RecipientEnergyParty/Identification", expectedReceiverId);
        return this;
    }

    public IAssertAggregationResultDocument HasTimestamp(string expectedTimestamp)
    {
        _documentAsserter.HasValue("HeaderEnergyDocument/Creation", expectedTimestamp);
        return this;
    }

    public IAssertAggregationResultDocument HasTransactionId(Guid expectedTransactionId)
    {
        _documentAsserter.HasValue($"PayloadEnergyTimeSeries[1]/Identification", expectedTransactionId.ToString("N"));
        return this;
    }

    public IAssertAggregationResultDocument HasGridAreaCode(string expectedGridAreaCode)
    {
        _documentAsserter.HasValue("PayloadEnergyTimeSeries[1]/MeteringGridAreaUsedDomainLocation/Identification", expectedGridAreaCode);
        return this;
    }

    public IAssertAggregationResultDocument HasBalanceResponsibleNumber(string expectedBalanceResponsibleNumber)
    {
        _documentAsserter.HasValue("PayloadEnergyTimeSeries[1]/BalanceResponsibleEnergyParty/Identification", expectedBalanceResponsibleNumber);
        return this;
    }

    public IAssertAggregationResultDocument HasEnergySupplierNumber(string expectedEnergySupplierNumber)
    {
        _documentAsserter.HasValue("PayloadEnergyTimeSeries[1]/BalanceSupplierEnergyParty/Identification", expectedEnergySupplierNumber);
        return this;
    }

    public IAssertAggregationResultDocument HasProductCode(string expectedProductCode)
    {
        _documentAsserter.HasValue("PayloadEnergyTimeSeries[1]/IncludedProductCharacteristic/Identification", expectedProductCode);
        return this;
    }

    public IAssertAggregationResultDocument HasPeriod(Period expectedPeriod)
    {
        ArgumentNullException.ThrowIfNull(expectedPeriod);
        _documentAsserter
            .HasValue("PayloadEnergyTimeSeries[1]/ObservationTimeSeriesPeriod/Start", expectedPeriod.StartToEbixString())
            .HasValue("PayloadEnergyTimeSeries[1]/ObservationTimeSeriesPeriod/End", expectedPeriod.EndToEbixString());
        return this;
    }

    public IAssertAggregationResultDocument HasPoint(int position, int quantity)
    {
        _documentAsserter
            .HasValue("PayloadEnergyTimeSeries[1]/IntervalEnergyObservation[1]/Position", position.ToString(CultureInfo.InvariantCulture))
            .HasValue("PayloadEnergyTimeSeries[1]/IntervalEnergyObservation[1]/EnergyQuantity", quantity.ToString(CultureInfo.InvariantCulture));
        return this;
    }

    public async Task<IAssertAggregationResultDocument> DocumentIsValidAsync()
    {
        await _documentAsserter.HasValidStructureAsync(DocumentType.AggregationResult, "3").ConfigureAwait(false);
        return this;
    }

    public IAssertAggregationResultDocument SettlementMethodIsNotPresent()
    {
        _documentAsserter.IsNotPresent("PayloadEnergyTimeSeries[1]/DetailMeasurementMeteringPointCharacteristic/SettlementMethod");
        return this;
    }

    public IAssertAggregationResultDocument EnergySupplierNumberIsNotPresent()
    {
        _documentAsserter.IsNotPresent("PayloadEnergyTimeSeries[1]/BalanceSupplierEnergyParty/Identification");
        return this;
    }

    public IAssertAggregationResultDocument BalanceResponsibleNumberIsNotPresent()
    {
        _documentAsserter.IsNotPresent("PayloadEnergyTimeSeries[1]/BalanceResponsibleEnergyParty/Identification");
        return this;
    }

    public IAssertAggregationResultDocument QuantityIsNotPresentForPosition(int position)
    {
        _documentAsserter.IsNotPresent($"PayloadEnergyTimeSeries[1]/IntervalEnergyObservation[{position}]/EnergyQuantity");
        return this;
    }

    public IAssertAggregationResultDocument QualityIsNotPresentForPosition(int position)
    {
        _documentAsserter.IsNotPresent($"PayloadEnergyTimeSeries[1]/IntervalEnergyObservation[{position}]/QuantityQuality");
        _documentAsserter.HasValue($"PayloadEnergyTimeSeries[1]/IntervalEnergyObservation[{position}]/QuantityMissing", "true");
        return this;
    }

    public IAssertAggregationResultDocument HasBusinessReason(BusinessReason businessReason)
    {
        ArgumentNullException.ThrowIfNull(businessReason, nameof(businessReason));
        _documentAsserter.HasValue("ProcessEnergyContext/EnergyBusinessProcess", EbixCode.Of<BusinessReason>(businessReason.Name));
        return this;
    }

    public IAssertAggregationResultDocument HasSettlementVersion(SettlementVersion settlementVersion)
    {
        ArgumentNullException.ThrowIfNull(settlementVersion, nameof(settlementVersion));
        _documentAsserter.HasValue("ProcessEnergyContext/ProcessVariant", EbixCode.Of<SettlementVersion>(settlementVersion.Name));
        return this;
    }

    public IAssertAggregationResultDocument SettlementVersionIsNotPresent()
    {
        _documentAsserter.IsNotPresent("ProcessEnergyContext/ProcessVariant");
        return this;
    }

    public IAssertAggregationResultDocument HasOriginalTransactionIdReference(string originalTransactionIdReference)
    {
        _documentAsserter.HasValue("PayloadEnergyTimeSeries[1]/OriginalBusinessDocument", originalTransactionIdReference);
        return this;
    }

    public IAssertAggregationResultDocument HasSettlementMethod(SettlementType settlementMethod)
    {
        ArgumentNullException.ThrowIfNull(settlementMethod, nameof(settlementMethod));
        _documentAsserter.HasValue("PayloadEnergyTimeSeries[1]/DetailMeasurementMeteringPointCharacteristic/SettlementMethod", EbixCode.Of<SettlementType>(settlementMethod.Name));
        return this;
    }
}
