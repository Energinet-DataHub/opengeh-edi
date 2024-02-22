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
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Application.MarketDocuments.Ebix;
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
        _documentAsserter.HasValueWithAttributes(
            "HeaderEnergyDocument/DocumentType",
            "E31",
            CreateRequiredListAttributes(CodeListType.Ebix));
    }

    #region header validation

    public IAssertWholesaleCalculationResultDocument HasMessageId(string expectedMessageId)
    {
        _documentAsserter.HasValue($"{HeaderEnergyDocument}/Identification", expectedMessageId);
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasBusinessReason(
        BusinessReason businessReason,
        CodeListType codeListType)
    {
        _documentAsserter.HasValueWithAttributes(
            $"{ProcessEnergyContext}/EnergyBusinessProcess",
            EbixCode.Of(businessReason),
            CreateRequiredListAttributes(codeListType));

        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasSenderId(ActorNumber expectedSenderId)
    {
        CreateRequiredSchemeAttribute(expectedSenderId);

        _documentAsserter.HasValueWithAttributes(
            $"{HeaderEnergyDocument}/SenderEnergyParty/Identification",
            expectedSenderId.Value,
            CreateRequiredSchemeAttribute(expectedSenderId));

        return this;
    }

    // TODO: Where to find this in the ebIX document?
    public IAssertWholesaleCalculationResultDocument HasSenderRole(ActorRole expectedSenderRole)
    {
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasReceiverId(ActorNumber expectedReceiverId)
    {
        _documentAsserter.HasValueWithAttributes(
            $"{HeaderEnergyDocument}/RecipientEnergyParty/Identification",
            expectedReceiverId.Value,
            CreateRequiredSchemeAttribute(expectedReceiverId));

        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasReceiverRole(ActorRole expectedReceiverRole)
    {
        _documentAsserter.HasValueWithAttributes(
            $"{ProcessEnergyContext}/EnergyBusinessProcessRole",
            EbixCode.Of(expectedReceiverRole),
            CreateRequiredListAttributes(CodeListType.EbixDenmark));

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
        _documentAsserter.HasValueWithAttributes(
            $"{PayloadEnergyTimeSeries}[1]/ChargeTypeOwnerEnergyParty/Identification",
            expectedChargeTypeOwner.Value,
            CreateRequiredSchemeAttribute(expectedChargeTypeOwner));
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasChargeCode(string expectedChargeTypeNumber)
    {
        _documentAsserter.HasValue($"{PayloadEnergyTimeSeries}[1]/PartyChargeTypeID", expectedChargeTypeNumber);
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasChargeType(ChargeType expectedChargeType)
    {
        _documentAsserter.HasValueWithAttributes(
            $"{PayloadEnergyTimeSeries}[1]/ChargeType",
            EbixCode.Of(expectedChargeType),
            CreateRequiredListAttributes(CodeListType.EbixDenmark));

        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasGridAreaCode(string expectedGridAreaCode)
    {
        _documentAsserter.HasValueWithAttributes(
            $"{PayloadEnergyTimeSeries}[1]/MeteringGridAreaUsedDomainLocation/Identification",
            expectedGridAreaCode,
            new AttributeNameAndValue("schemeAgencyIdentifier", EbixDocumentWriter.EbixCodeList),
            new AttributeNameAndValue("schemeIdentifier", EbixDocumentWriter.CountryCodeDenmark));

        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasEnergySupplierNumber(ActorNumber expectedEnergySupplierNumber)
    {
        _documentAsserter.HasValueWithAttributes(
            $"{PayloadEnergyTimeSeries}[1]/BalanceSupplierEnergyParty/Identification",
            expectedEnergySupplierNumber.Value,
            CreateRequiredSchemeAttribute(expectedEnergySupplierNumber));
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasProductCode(string expectedProductCode)
    {
        _documentAsserter.HasValueWithAttributes(
            $"{PayloadEnergyTimeSeries}[1]/IncludedProductCharacteristic/Identification",
            expectedProductCode,
            new AttributeNameAndValue("listAgencyIdentifier", "9"));

        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasMeasurementUnit(MeasurementUnit expectedMeasurementUnit)
    {
        _documentAsserter.HasValueWithAttributes(
            $"{PayloadEnergyTimeSeries}[1]/IncludedProductCharacteristic/UnitType",
            EbixCode.Of(expectedMeasurementUnit),
            CreateRequiredListAttributes(CodeListType.Ebix));
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasPriceMeasurementUnit(MeasurementUnit expectedPriceMeasurementUnit)
    {
        // Is not used in ebIX document
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasCurrency(Currency expectedPriceUnit)
    {
        _documentAsserter.HasValueWithAttributes(
            $"{PayloadEnergyTimeSeries}[1]/Currency",
            EbixCode.Of(expectedPriceUnit),
            CreateRequiredListAttributes(CodeListType.Ebix));
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

    public IAssertWholesaleCalculationResultDocument SettlementMethodIsNotPresent()
    {
        // TODO: Not used in monthly (månedssum), implement later
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
        // TODO: Not used in monthly (månedssum), implement later
        return this;
    }

    public IAssertWholesaleCalculationResultDocument SettlementVersionIsNotPresent()
    {
        // TODO: Not used in monthly (månedssum), implement later
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasOriginalTransactionIdReference(string originalTransactionIdReference)
    {
        // Not supported in ebIX, since ebIX has no requests (anmodning)
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasEvaluationType(string expectedMarketEvaluationType)
    {
        // TODO: Not used in monthly (månedssum), implement later
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasSettlementMethod(SettlementType settlementMethod)
    {
        // TODO: Not used in monthly (månedssum), implement later
        return this;
    }

    #endregion

    private static AttributeNameAndValue[] CreateRequiredListAttributes(CodeListType codeListType)
    {
        var (codeList, countryCode) = GetCodeListConstant(codeListType);

        var requiredAttributes = new List<AttributeNameAndValue>
        {
            new("listAgencyIdentifier", codeList),
        };

        if (!string.IsNullOrEmpty(countryCode))
            requiredAttributes.Add(new("listIdentifier", countryCode));

        return requiredAttributes.ToArray();
    }

    private static (string CodeList, string? CountryCode) GetCodeListConstant(CodeListType codeListType) =>
        codeListType switch
        {
            CodeListType.UnitedNations => (EbixDocumentWriter.UnitedNationsCodeList, null),
            CodeListType.Ebix => (EbixDocumentWriter.EbixCodeList, null),
            CodeListType.EbixDenmark => (EbixDocumentWriter.EbixCodeList, EbixDocumentWriter.CountryCodeDenmark),
            _ => throw new ArgumentOutOfRangeException(nameof(codeListType), codeListType, "Invalid CodeListType"),
        };

    private static AttributeNameAndValue CreateRequiredSchemeAttribute(ActorNumber actorNumber)
    {
        var codeOwner = GetActorNumberOwner(ActorNumber.IsGlnNumber(actorNumber.Value) ? ActorNumberType.Gln : ActorNumberType.Eic);
        var requiredAttribute = new AttributeNameAndValue("schemeAgencyIdentifier", codeOwner);

        return requiredAttribute;
    }

    private static string GetActorNumberOwner(ActorNumberType actorNumberType) => actorNumberType switch
    {
        ActorNumberType.Gln => EbixDocumentWriter.Gs1Code,
        ActorNumberType.Eic => EbixDocumentWriter.EicCode,
        _ => throw new ArgumentOutOfRangeException(nameof(actorNumberType), actorNumberType, "Invalid ActorNumberType"),
    };
}
