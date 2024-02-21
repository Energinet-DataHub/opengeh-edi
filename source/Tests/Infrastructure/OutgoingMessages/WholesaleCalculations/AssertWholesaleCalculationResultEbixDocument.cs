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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.MarketDocuments;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.Asserts;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.WholesaleCalculations;

internal sealed class AssertWholesaleCalculationResultEbixDocument : IAssertWholesaleCalculationResultDocument
{
    private const string HeaderEnergyDocument = "HeaderEnergyDocument";
    private const string ProcessEnergyContext = "ProcessEnergyContext";
    private const string PayloadEnergyTimeSeries = "PayloadEnergyTimeSeries";

    private readonly AssertEbixDocument _documentAsserter;

    public AssertWholesaleCalculationResultEbixDocument(AssertEbixDocument documentAsserter)
    {
        _documentAsserter = documentAsserter;
        _documentAsserter.HasValue("HeaderEnergyDocument/DocumentType", "E31");
    }

    #region header validation

    public IAssertWholesaleCalculationResultDocument HasMessageId(string expectedMessageId)
    {
        _documentAsserter.HasValue($"{HeaderEnergyDocument}/Identification", expectedMessageId);
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasBusinessReason(BusinessReason businessReason)
    {
        _documentAsserter.HasValue($"{ProcessEnergyContext}/EnergyBusinessProcess", EbixCode.Of(businessReason));
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasSenderId(ActorNumber expectedSenderId)
    {
        _documentAsserter.HasValue($"{HeaderEnergyDocument}/SenderEnergyParty/Identification", expectedSenderId.Value);
        return this;
    }

    // TODO: Where to find this in the ebIX document?
    public IAssertWholesaleCalculationResultDocument HasSenderRole(ActorRole expectedSenderRole)
    {
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasReceiverId(ActorNumber expectedReceiverId)
    {
        _documentAsserter.HasValue($"{HeaderEnergyDocument}/RecipientEnergyParty/Identification", expectedReceiverId.Value);
        return this;
    }

    // TODO: Where to find this in the ebIX document?
    public IAssertWholesaleCalculationResultDocument HasReceiverRole(ActorRole expectedReceiverRole)
    {
        _documentAsserter.HasValue($"{ProcessEnergyContext}/EnergyBusinessProcessRole", EbixCode.Of(expectedReceiverRole));
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasTimestamp(string expectedTimestamp)
    {
        _documentAsserter.HasValue($"{HeaderEnergyDocument}/Creation", expectedTimestamp);
        return this;
    }

    #endregion

    #region series validation

    public IAssertWholesaleCalculationResultDocument HasTransactionId(Guid expectedTransactionId)
    {
        _documentAsserter.HasValue($"{PayloadEnergyTimeSeries}[1]/Identification", expectedTransactionId.ToString("N"));
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasCalculationVersion(int expectedVersion)
    {
        _documentAsserter.HasValue($"{PayloadEnergyTimeSeries}[1]/Version", expectedVersion.ToString(CultureInfo.InvariantCulture));
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasChargeTypeOwner(ActorNumber expectedChargeTypeOwner)
    {
        _documentAsserter.HasValue($"{PayloadEnergyTimeSeries}[1]/ChargeTypeOwnerEnergyParty/Identification", expectedChargeTypeOwner.Value);
        return this;
    }

    // TODO: Where to find this in the ebIX document?
    public IAssertWholesaleCalculationResultDocument HasChargeCode(string expectedChargeTypeNumber)
    {
        _documentAsserter.HasValue($"{PayloadEnergyTimeSeries}[1]/PartyChargeTypeID", expectedChargeTypeNumber);
        return this;
    }

    // TODO: Where to find this in the ebIX document?
    public IAssertWholesaleCalculationResultDocument HasChargeType(ChargeType expectedChargeType)
    {
        _documentAsserter.HasValue($"{PayloadEnergyTimeSeries}[1]/ChargeType",  EbixCode.Of(expectedChargeType));
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasGridAreaCode(string expectedGridAreaCode)
    {
        _documentAsserter.HasValue($"{PayloadEnergyTimeSeries}[1]/MeteringGridAreaUsedDomainLocation/Identification", expectedGridAreaCode);
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasEnergySupplierNumber(ActorNumber expectedEnergySupplierNumber)
    {
        _documentAsserter.HasValue($"{PayloadEnergyTimeSeries}[1]/BalanceSupplierEnergyParty/Identification", expectedEnergySupplierNumber.Value);
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasProductCode(string expectedProductCode)
    {
        _documentAsserter.HasValue($"{PayloadEnergyTimeSeries}[1]/IncludedProductCharacteristic/Identification", expectedProductCode);
        return this;
    }

    // TODO: Where to find this in the ebIX document?
    public IAssertWholesaleCalculationResultDocument HasMeasurementUnit(MeasurementUnit expectedMeasurementUnit)
    {
        _documentAsserter.HasValue($"{PayloadEnergyTimeSeries}[1]/IncludedProductCharacteristic/UnitType", EbixCode.Of(expectedMeasurementUnit));
        return this;
    }

    // TODO: Where to find this in the ebIX document?
    public IAssertWholesaleCalculationResultDocument HasPriceMeasurementUnit(MeasurementUnit expectedPriceMeasurementUnit)
    {
        _documentAsserter.HasValue($"{PayloadEnergyTimeSeries}[1]/IncludedProductCharacteristic/UnitType", EbixCode.Of(expectedPriceMeasurementUnit));
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasCurrency(Currency expectedPriceUnit)
    {
        _documentAsserter.HasValue($"{PayloadEnergyTimeSeries}[1]/Currency", EbixCode.Of(expectedPriceUnit));
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasPeriod(Period expectedPeriod)
    {
        _documentAsserter
            .HasValue($"{PayloadEnergyTimeSeries}[1]/ObservationTimeSeriesPeriod/Start", expectedPeriod.StartToEbixString())
            .HasValue($"{PayloadEnergyTimeSeries}[1]/ObservationTimeSeriesPeriod/End", expectedPeriod.EndToEbixString());
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasResolution(Resolution resolution)
    {
        _documentAsserter.HasValue($"{PayloadEnergyTimeSeries}[1]/ObservationTimeSeriesPeriod/ResolutionDuration", EbixCode.Of(resolution));
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasPositionAndQuantity(int expectedPosition, int expectedQuantity)
    {
        _documentAsserter
            .HasValue($"{PayloadEnergyTimeSeries}[1]/IntervalEnergyObservation[1]/Position", expectedPosition.ToString(CultureInfo.InvariantCulture))
            .HasValue($"{PayloadEnergyTimeSeries}[1]/IntervalEnergyObservation[1]/EnergySum", expectedQuantity.ToString(CultureInfo.InvariantCulture));
        return this;
    }

    public async Task<IAssertWholesaleCalculationResultDocument> DocumentIsValidAsync()
    {
        await _documentAsserter.HasValidStructureAsync(DocumentType.NotifyWholesaleServices, "3").ConfigureAwait(false);
        return this;
    }

    // TODO: Where to find this in the ebIX document?
    public IAssertWholesaleCalculationResultDocument SettlementMethodIsNotPresent()
    {
        _documentAsserter.IsNotPresent($"{PayloadEnergyTimeSeries}[1]/DetailMeasurementMeteringPointCharacteristic/SettlementMethod");
        return this;
    }

    public IAssertWholesaleCalculationResultDocument QualityIsPresentForPosition(
        int position,
        string quantityQualityCode)
    {
        _documentAsserter.HasValue(
            $"{PayloadEnergyTimeSeries}[1]/IntervalEnergyObservation[{position}]/QuantityQuality",
            quantityQualityCode);

        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasSettlementVersion(SettlementVersion settlementVersion)
    {
        _documentAsserter.HasValue($"{ProcessEnergyContext}/ProcessVariant", EbixCode.Of(settlementVersion));
        return this;
    }

    public IAssertWholesaleCalculationResultDocument SettlementVersionIsNotPresent()
    {
        _documentAsserter.IsNotPresent($"{ProcessEnergyContext}/ProcessVariant");
        return this;
    }

    // TODO: Where to find this in the ebIX document?
    public IAssertWholesaleCalculationResultDocument HasOriginalTransactionIdReference(string originalTransactionIdReference)
    {
        _documentAsserter.HasValue("PayloadEnergyTimeSeries[1]/OriginalBusinessDocument", originalTransactionIdReference);
        return this;
    }

    // TODO: Where to find this in the ebIX document?
    public IAssertWholesaleCalculationResultDocument HasEvaluationType(string expectedMarketEvaluationType)
    {
        throw new NotImplementedException();
    }

    // TODO: Where to find this in the ebIX document?
    public IAssertWholesaleCalculationResultDocument HasSettlementMethod(SettlementType settlementMethod)
    {
        _documentAsserter.HasValue("PayloadEnergyTimeSeries[1]/DetailMeasurementMeteringPointCharacteristic/SettlementMethod", EbixCode.Of(settlementMethod));
        return this;
    }

    #endregion
}
