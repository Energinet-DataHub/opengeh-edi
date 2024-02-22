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

public class AssertWholesaleCalculationResultXmlDocument : IAssertWholesaleCalculationResultDocument
{
    private readonly AssertXmlDocument _documentAsserter;

    public AssertWholesaleCalculationResultXmlDocument(AssertXmlDocument documentAsserter)
    {
        _documentAsserter = documentAsserter;
        _documentAsserter.HasValue("type", "E31");
    }

    public async Task<IAssertWholesaleCalculationResultDocument> DocumentIsValidAsync()
    {
        await _documentAsserter.HasValidStructureAsync(DocumentType.NotifyWholesaleServices).ConfigureAwait(false);
        return this;
    }

    #region header validation
    public IAssertWholesaleCalculationResultDocument HasMessageId(string expectedMessageId)
    {
        _documentAsserter.HasValue("mRID", expectedMessageId);
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasBusinessReason(
        BusinessReason expectedBusinessReason,
        CodeListType codeListType)
    {
        _documentAsserter.HasValue("process.processType", CimCode.Of(expectedBusinessReason));
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasSenderId(ActorNumber expectedSenderId)
    {
        ArgumentNullException.ThrowIfNull(expectedSenderId);
        _documentAsserter.HasValue("sender_MarketParticipant.mRID", expectedSenderId.Value);
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasSenderRole(ActorRole expectedSenderRole)
    {
        ArgumentNullException.ThrowIfNull(expectedSenderRole);
        _documentAsserter.HasValue("sender_MarketParticipant.marketRole.type", expectedSenderRole.Code);
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasReceiverId(ActorNumber expectedReceiverId)
    {
        ArgumentNullException.ThrowIfNull(expectedReceiverId);
        _documentAsserter.HasValue("receiver_MarketParticipant.mRID", expectedReceiverId.Value);
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasReceiverRole(ActorRole expectedReceiverRole)
    {
        ArgumentNullException.ThrowIfNull(expectedReceiverRole);
        _documentAsserter.HasValue("receiver_MarketParticipant.marketRole.type", expectedReceiverRole.Code);
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasTimestamp(string expectedTimestamp)
    {
        _documentAsserter.HasValue("createdDateTime", expectedTimestamp);
        return this;
    }

    #endregion

    public IAssertWholesaleCalculationResultDocument HasTransactionId(Guid expectedTransactionId)
    {
        _documentAsserter.HasValue($"Series[1]/mRID", expectedTransactionId.ToString());
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasCalculationVersion(int expectedVersion)
    {
        _documentAsserter.HasValue($"Series[1]/version", expectedVersion.ToString(CultureInfo.InvariantCulture));
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasSettlementVersion(SettlementVersion expectedSettlementVersion)
    {
        _documentAsserter.HasValue("Series[1]/settlement_Series.version", CimCode.Of(expectedSettlementVersion));
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasOriginalTransactionIdReference(string expectedOriginalTransactionIdReference)
    {
        _documentAsserter.HasValue("Series[1]/originalTransactionIDReference_Series.mRID", expectedOriginalTransactionIdReference);
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasEvaluationType(string expectedMarketEvaluationType)
    {
        _documentAsserter.HasValue("Series[1]/marketEvaluationPoint.type", expectedMarketEvaluationType);
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasSettlementMethod(SettlementType expectedSettlementMethod)
    {
        _documentAsserter.HasValue("Series[1]/marketEvaluationPoint.settlementMethod", CimCode.Of(expectedSettlementMethod));
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasChargeCode(string expectedChargeTypeNumber)
    {
        _documentAsserter.HasValue("Series[1]/chargeType.mRID", expectedChargeTypeNumber);
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasChargeType(ChargeType expectedChargeType)
    {
        ArgumentNullException.ThrowIfNull(expectedChargeType);
        _documentAsserter.HasValue("Series[1]/chargeType.type", expectedChargeType.Code);
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasChargeTypeOwner(ActorNumber expectedChargeTypeOwner)
    {
        ArgumentNullException.ThrowIfNull(expectedChargeTypeOwner);
        _documentAsserter.HasValue("Series[1]/chargeType.chargeTypeOwner_MarketParticipant.mRID", expectedChargeTypeOwner.Value);
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasGridAreaCode(string expectedGridAreaCode)
    {
        _documentAsserter.HasValue("Series[1]/meteringGridArea_Domain.mRID", expectedGridAreaCode);
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasEnergySupplierNumber(ActorNumber expectedEnergySupplierNumber)
    {
        ArgumentNullException.ThrowIfNull(expectedEnergySupplierNumber);
        _documentAsserter.HasValue("Series[1]/energySupplier_MarketParticipant.mRID", expectedEnergySupplierNumber.Value);
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasProductCode(string expectedProductCode)
    {
        _documentAsserter.HasValue("Series[1]/product", expectedProductCode);
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasMeasurementUnit(MeasurementUnit expectedMeasurementUnit)
    {
        ArgumentNullException.ThrowIfNull(expectedMeasurementUnit);
        _documentAsserter.HasValue("Series[1]/quantity_Measure_Unit.name", expectedMeasurementUnit.Code);
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasPriceMeasurementUnit(MeasurementUnit expectedPriceMeasurementUnit)
    {
        ArgumentNullException.ThrowIfNull(expectedPriceMeasurementUnit);
        _documentAsserter.HasValue("Series[1]/price_Measure_Unit.name", expectedPriceMeasurementUnit.Code);
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasCurrency(Currency expectedPriceUnit)
    {
        ArgumentNullException.ThrowIfNull(expectedPriceUnit);
        _documentAsserter.HasValue("Series[1]/currency_Unit.name", expectedPriceUnit.Code);
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasPeriod(Period expectedPeriod)
    {
        ArgumentNullException.ThrowIfNull(expectedPeriod);
        _documentAsserter
            .HasValue("Series[1]/Period/timeInterval/start", expectedPeriod.StartToString())
            .HasValue("Series[1]/Period/timeInterval/end", expectedPeriod.EndToString());
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasResolution(Resolution resolution)
    {
        ArgumentNullException.ThrowIfNull(resolution);
        _documentAsserter.HasValue("Series[1]/Period/resolution", resolution.Code);
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasPositionAndQuantity(int expectedPosition, int expectedQuantity)
    {
        _documentAsserter
            .HasValue("Series[1]/Period/Point[1]/position", expectedPosition.ToString(CultureInfo.InvariantCulture))
            .HasValue("Series[1]/Period/Point[1]/energySum_Quantity.quantity", expectedQuantity.ToString(CultureInfo.InvariantCulture));
        return this;
    }

    public IAssertWholesaleCalculationResultDocument SettlementMethodIsNotPresent()
    {
        _documentAsserter.IsNotPresent("Series[1]/marketEvaluationPoint.settlementMethod");
        return this;
    }

    public IAssertWholesaleCalculationResultDocument QualityIsPresentForPosition(int expectedPosition, string expectedQuantityQualityCode)
    {
        _documentAsserter.HasValue($"Series[1]/Period/Point[{expectedPosition}]/quality", expectedQuantityQualityCode);
        return this;
    }

    public IAssertWholesaleCalculationResultDocument SettlementVersionIsNotPresent()
    {
        _documentAsserter.IsNotPresent("Series[1]/settlement_Series.version");
        return this;
    }
}
