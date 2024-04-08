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

using System.Globalization;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using OutgoingMessages.Application.Tests.MarketDocuments.Asserts;

namespace OutgoingMessages.Application.Tests.MarketDocuments.NotifyWholesaleServices;

public class AssertNotifyWholesaleServicesXmlDocument : IAssertNotifyWholesaleServicesDocument
{
    private readonly AssertXmlDocument _documentAsserter;

    public AssertNotifyWholesaleServicesXmlDocument(AssertXmlDocument documentAsserter)
    {
        _documentAsserter = documentAsserter;
        _documentAsserter.HasValue("type", "E31");
    }

    public async Task<IAssertNotifyWholesaleServicesDocument> DocumentIsValidAsync()
    {
        await _documentAsserter.HasValidStructureAsync(DocumentType.NotifyWholesaleServices).ConfigureAwait(false);
        return this;
    }

    #region header validation
    public IAssertNotifyWholesaleServicesDocument HasMessageId(string expectedMessageId)
    {
        _documentAsserter.HasValue("mRID", expectedMessageId);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasMessageId()
    {
        _documentAsserter.HasValue("mRID");
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasBusinessReason(
        BusinessReason expectedBusinessReason,
        CodeListType codeListType)
    {
        ArgumentNullException.ThrowIfNull(expectedBusinessReason);
        _documentAsserter.HasValue("process.processType", expectedBusinessReason.Code);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasSenderId(ActorNumber expectedSenderId, string codingScheme)
    {
        ArgumentNullException.ThrowIfNull(expectedSenderId);
        ArgumentException.ThrowIfNullOrWhiteSpace(codingScheme);

        _documentAsserter.HasValue("sender_MarketParticipant.mRID", expectedSenderId.Value);
        _documentAsserter.HasAttribute("sender_MarketParticipant.mRID", "codingScheme", codingScheme);

        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasSenderRole(ActorRole expectedSenderRole)
    {
        ArgumentNullException.ThrowIfNull(expectedSenderRole);
        _documentAsserter.HasValue("sender_MarketParticipant.marketRole.type", expectedSenderRole.Code);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasReceiverId(ActorNumber expectedReceiverId)
    {
        ArgumentNullException.ThrowIfNull(expectedReceiverId);
        _documentAsserter.HasValue("receiver_MarketParticipant.mRID", expectedReceiverId.Value);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasReceiverRole(ActorRole expectedReceiverRole, CodeListType codeListType)
    {
        ArgumentNullException.ThrowIfNull(expectedReceiverRole);
        _documentAsserter.HasValue("receiver_MarketParticipant.marketRole.type", expectedReceiverRole.Code);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasTimestamp(string expectedTimestamp)
    {
        _documentAsserter.HasValue("createdDateTime", expectedTimestamp);
        return this;
    }

    #endregion

    public IAssertNotifyWholesaleServicesDocument HasTransactionId(Guid expectedTransactionId)
    {
        _documentAsserter.HasValue($"Series[1]/mRID", expectedTransactionId.ToString());
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasTransactionId()
    {
        _documentAsserter.HasValue($"Series[1]/mRID");
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasCalculationVersion(long expectedVersion)
    {
        _documentAsserter.HasValue($"Series[1]/version", expectedVersion.ToString(CultureInfo.InvariantCulture));
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasSettlementVersion(SettlementVersion expectedSettlementVersion)
    {
        ArgumentNullException.ThrowIfNull(expectedSettlementVersion);
        _documentAsserter.HasValue("Series[1]/settlement_Series.version", expectedSettlementVersion.Code);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasOriginalTransactionIdReference(
        string expectedOriginalTransactionIdReference)
    {
        _documentAsserter.HasValue(
            "Series[1]/originalTransactionIDReference_Series.mRID",
            expectedOriginalTransactionIdReference);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasSettlementMethod(SettlementMethod expectedSettlementMethod)
    {
        ArgumentNullException.ThrowIfNull(expectedSettlementMethod);
        _documentAsserter.HasValue("Series[1]/marketEvaluationPoint.settlementMethod", expectedSettlementMethod.Code);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument PriceAmountIsPresentForPointIndex(int pointIndex, string? expectedPrice)
    {
        _documentAsserter.HasValue($"Series[1]/Period/Point[{pointIndex + 1}]/price.amount", expectedPrice ?? "0");
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasMeteringPointType(MeteringPointType expectedMeteringPointType)
    {
        ArgumentNullException.ThrowIfNull(expectedMeteringPointType);
        _documentAsserter.HasValue("Series[1]/marketEvaluationPoint.type", expectedMeteringPointType.Code);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasChargeCode(string expectedChargeTypeNumber)
    {
        _documentAsserter.HasValue("Series[1]/chargeType.mRID", expectedChargeTypeNumber);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasChargeType(ChargeType expectedChargeType)
    {
        ArgumentNullException.ThrowIfNull(expectedChargeType);
        _documentAsserter.HasValue("Series[1]/chargeType.type", expectedChargeType.Code);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasChargeTypeOwner(
        ActorNumber expectedChargeTypeOwner,
        string codingScheme)
    {
        ArgumentNullException.ThrowIfNull(expectedChargeTypeOwner);
        _documentAsserter.HasValue("Series[1]/chargeType.chargeTypeOwner_MarketParticipant.mRID", expectedChargeTypeOwner.Value);
        _documentAsserter.HasAttribute(
            "Series[1]/chargeType.chargeTypeOwner_MarketParticipant.mRID",
            "codingScheme",
            codingScheme);

        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasGridAreaCode(string expectedGridAreaCode, string codingScheme)
    {
        _documentAsserter.HasValue("Series[1]/meteringGridArea_Domain.mRID", expectedGridAreaCode);
        _documentAsserter.HasAttribute("Series[1]/meteringGridArea_Domain.mRID", "codingScheme", codingScheme);

        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasEnergySupplierNumber(
        ActorNumber expectedEnergySupplierNumber,
        string codingScheme)
    {
        ArgumentNullException.ThrowIfNull(expectedEnergySupplierNumber);
        _documentAsserter.HasValue("Series[1]/energySupplier_MarketParticipant.mRID", expectedEnergySupplierNumber.Value);
        _documentAsserter.HasAttribute("Series[1]/energySupplier_MarketParticipant.mRID", "codingScheme", codingScheme);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasProductCode(string expectedProductCode)
    {
        _documentAsserter.HasValue("Series[1]/product", expectedProductCode);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasMeasurementUnit(MeasurementUnit expectedMeasurementUnit)
    {
        ArgumentNullException.ThrowIfNull(expectedMeasurementUnit);
        _documentAsserter.HasValue("Series[1]/quantity_Measure_Unit.name", expectedMeasurementUnit.Code);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasPriceMeasurementUnit(MeasurementUnit expectedPriceMeasurementUnit)
    {
        ArgumentNullException.ThrowIfNull(expectedPriceMeasurementUnit);
        _documentAsserter.HasValue("Series[1]/price_Measure_Unit.name", expectedPriceMeasurementUnit.Code);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasCurrency(Currency expectedPriceUnit)
    {
        ArgumentNullException.ThrowIfNull(expectedPriceUnit);
        _documentAsserter.HasValue("Series[1]/currency_Unit.name", expectedPriceUnit.Code);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasPeriod(Period expectedPeriod)
    {
        ArgumentNullException.ThrowIfNull(expectedPeriod);
        _documentAsserter
            .HasValue("Series[1]/Period/timeInterval/start", expectedPeriod.StartToString())
            .HasValue("Series[1]/Period/timeInterval/end", expectedPeriod.EndToString());
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasResolution(Resolution resolution)
    {
        ArgumentNullException.ThrowIfNull(resolution);
        _documentAsserter.HasValue("Series[1]/Period/resolution", resolution.Code);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasPositionAndQuantity(int expectedPosition, decimal expectedQuantity)
    {
        _documentAsserter
            .HasValue("Series[1]/Period/Point[1]/position", expectedPosition.ToString(CultureInfo.InvariantCulture))
            .HasValue("Series[1]/Period/Point[1]/energySum_Quantity.quantity", expectedQuantity.ToString(CultureInfo.InvariantCulture));
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument SettlementMethodIsNotPresent()
    {
        _documentAsserter.IsNotPresent("Series[1]/marketEvaluationPoint.settlementMethod");
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument QualityIsPresentForPosition(int expectedPosition, string expectedQuantityQualityCode)
    {
        _documentAsserter.HasValue($"Series[1]/Period/Point[{expectedPosition}]/quality", expectedQuantityQualityCode);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument SettlementVersionIsNotPresent()
    {
        _documentAsserter.IsNotPresent("Series[1]/settlement_Series.version");
        return this;
    }
}
