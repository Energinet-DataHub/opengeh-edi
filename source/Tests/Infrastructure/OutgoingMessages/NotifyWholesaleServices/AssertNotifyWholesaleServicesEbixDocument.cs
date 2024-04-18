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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats.Ebix;
using Energinet.DataHub.Edi.Responses;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.Asserts;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;
using Resolution = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Resolution;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.NotifyWholesaleServices;

[SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Test class")]
public sealed class AssertNotifyWholesaleServicesEbixDocument : IAssertNotifyWholesaleServicesDocument
{
    private const string HeaderEnergyDocument = "HeaderEnergyDocument";
    private const string ProcessEnergyContext = "ProcessEnergyContext";
    private const string PayloadEnergyTimeSeries = "PayloadEnergyTimeSeries";

    private readonly AssertEbixDocument _documentAsserter;
    private readonly bool _skipIdentificationLengthValidation;

    public AssertNotifyWholesaleServicesEbixDocument(AssertEbixDocument documentAsserter, bool skipIdentificationLengthValidation = false)
    {
        _documentAsserter = documentAsserter;
        _skipIdentificationLengthValidation = skipIdentificationLengthValidation;
        _documentAsserter.HasValueWithAttributes(
            "HeaderEnergyDocument/DocumentType",
            "E31",
            CreateRequiredListAttributes(CodeListType.Ebix));
    }

    #region header validation

    public IAssertNotifyWholesaleServicesDocument HasMessageId(string expectedMessageId)
    {
        _documentAsserter.HasValue($"{HeaderEnergyDocument}/Identification", expectedMessageId);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument MessageIdExists()
    {
        _documentAsserter.ElementExists($"{HeaderEnergyDocument}/Identification");
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasBusinessReason(
        BusinessReason expectedBusinessReason,
        CodeListType codeListType)
    {
        _documentAsserter.HasValueWithAttributes(
            $"{ProcessEnergyContext}/EnergyBusinessProcess",
            EbixCode.Of(expectedBusinessReason),
            CreateRequiredListAttributes(codeListType));

        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasSenderId(ActorNumber expectedSenderId, string codingScheme)
    {
        CreateRequiredSchemeAttribute(expectedSenderId);

        _documentAsserter.HasValueWithAttributes(
            $"{HeaderEnergyDocument}/SenderEnergyParty/Identification",
            expectedSenderId.Value,
            CreateRequiredSchemeAttribute(expectedSenderId));

        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasSenderRole(ActorRole expectedSenderRole)
    {
        // SenderRole does not exist in the Ebix format
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasReceiverId(ActorNumber expectedReceiverId)
    {
        _documentAsserter.HasValueWithAttributes(
            $"{HeaderEnergyDocument}/RecipientEnergyParty/Identification",
            expectedReceiverId.Value,
            CreateRequiredSchemeAttribute(expectedReceiverId));

        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasReceiverRole(ActorRole expectedReceiverRole, CodeListType codeListType)
    {
        _documentAsserter.HasValueWithAttributes(
            $"{ProcessEnergyContext}/EnergyBusinessProcessRole",
            EbixCode.Of(expectedReceiverRole),
            CreateRequiredListAttributes(codeListType));

        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasTimestamp(string expectedTimestamp)
    {
        _documentAsserter.HasValue($"{HeaderEnergyDocument}/Creation", expectedTimestamp);
        return this;
    }

    #endregion

    #region series validation

    public IAssertNotifyWholesaleServicesDocument HasTransactionId(Guid expectedTransactionId)
    {
        _documentAsserter.HasValue($"{PayloadEnergyTimeSeries}[1]/Identification", expectedTransactionId.ToString("N"));
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument TransactionIdExists()
    {
        _documentAsserter.ElementExists($"{PayloadEnergyTimeSeries}[1]/Identification");
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasCalculationVersion(long expectedVersion)
    {
        _documentAsserter.HasValue($"{PayloadEnergyTimeSeries}[1]/Version", expectedVersion.ToString(CultureInfo.InvariantCulture));
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasChargeTypeOwner(ActorNumber expectedChargeTypeOwner, string codingScheme)
    {
        _documentAsserter.HasValueWithAttributes(
            $"{PayloadEnergyTimeSeries}[1]/ChargeTypeOwnerEnergyParty/Identification",
            expectedChargeTypeOwner.Value,
            CreateRequiredSchemeAttribute(expectedChargeTypeOwner));
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasMeteringPointType(MeteringPointType expectedMeteringPointType)
    {
        _documentAsserter.HasValue($"{PayloadEnergyTimeSeries}[1]/DetailMeasurementMeteringPointCharacteristic/TypeOfMeteringPoint", expectedMeteringPointType.Code);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasChargeCode(string expectedChargeTypeNumber)
    {
        _documentAsserter.HasValue($"{PayloadEnergyTimeSeries}[1]/PartyChargeTypeID", expectedChargeTypeNumber);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasChargeType(ChargeType expectedChargeType)
    {
        _documentAsserter.HasValueWithAttributes(
            $"{PayloadEnergyTimeSeries}[1]/ChargeType",
            EbixCode.Of(expectedChargeType),
            CreateRequiredListAttributes(CodeListType.EbixDenmark));

        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasGridAreaCode(string expectedGridAreaCode, string codingScheme)
    {
        _documentAsserter.HasValueWithAttributes(
            $"{PayloadEnergyTimeSeries}[1]/MeteringGridAreaUsedDomainLocation/Identification",
            expectedGridAreaCode,
            new AttributeNameAndValue("schemeAgencyIdentifier", EbixDocumentWriter.EbixCodeList),
            new AttributeNameAndValue("schemeIdentifier", EbixDocumentWriter.CountryCodeDenmark));

        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasEnergySupplierNumber(
        ActorNumber expectedEnergySupplierNumber,
        string codingScheme)
    {
        _documentAsserter.HasValueWithAttributes(
            $"{PayloadEnergyTimeSeries}[1]/BalanceSupplierEnergyParty/Identification",
            expectedEnergySupplierNumber.Value,
            CreateRequiredSchemeAttribute(expectedEnergySupplierNumber));
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasProductCode(string expectedProductCode)
    {
        _documentAsserter.HasValueWithAttributes(
            $"{PayloadEnergyTimeSeries}[1]/IncludedProductCharacteristic/Identification",
            expectedProductCode,
            new AttributeNameAndValue("listAgencyIdentifier", "9"));

        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasQuantityMeasurementUnit(MeasurementUnit expectedMeasurementUnit)
    {
        _documentAsserter.HasValueWithAttributes(
            $"{PayloadEnergyTimeSeries}[1]/IncludedProductCharacteristic/UnitType",
            EbixCode.Of(expectedMeasurementUnit),
            CreateRequiredListAttributes(CodeListType.Ebix));
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasPriceMeasurementUnit(MeasurementUnit expectedPriceMeasurementUnit)
    {
        // Is not used in ebIX document
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasCurrency(Currency expectedPriceUnit)
    {
        _documentAsserter.HasValueWithAttributes(
            $"{PayloadEnergyTimeSeries}[1]/Currency",
            EbixCode.Of(expectedPriceUnit),
            CreateRequiredListAttributes(CodeListType.Ebix));
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasPeriod(Period expectedPeriod)
    {
        _documentAsserter
            .HasValue($"{PayloadEnergyTimeSeries}[1]/ObservationTimeSeriesPeriod/Start", expectedPeriod.StartToEbixString())
            .HasValue($"{PayloadEnergyTimeSeries}[1]/ObservationTimeSeriesPeriod/End", expectedPeriod.EndToEbixString());
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasResolution(Resolution resolution)
    {
        _documentAsserter.HasValue($"{PayloadEnergyTimeSeries}[1]/ObservationTimeSeriesPeriod/ResolutionDuration", EbixCode.Of(resolution));
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasSumQuantityForPosition(int expectedPosition, int expectedSumQuantity)
    {
        _documentAsserter
            .HasValue($"{PayloadEnergyTimeSeries}[1]/IntervalEnergyObservation[{expectedPosition}]/Position", expectedPosition.ToString(CultureInfo.InvariantCulture))
            .HasValue($"{PayloadEnergyTimeSeries}[1]/IntervalEnergyObservation[{expectedPosition}]/EnergySum", expectedSumQuantity.ToString(CultureInfo.InvariantCulture));
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasQuantityForPosition(int expectedPosition, int expectedQuantity)
    {
        _documentAsserter
            .HasValue($"{PayloadEnergyTimeSeries}[1]/IntervalEnergyObservation[{expectedPosition}]/Position", expectedPosition.ToString(CultureInfo.InvariantCulture))
            .HasValue($"{PayloadEnergyTimeSeries}[1]/IntervalEnergyObservation[{expectedPosition}]/EnergyQuantity", expectedQuantity.ToString(CultureInfo.InvariantCulture));
        return this;
    }

    public async Task<IAssertNotifyWholesaleServicesDocument> DocumentIsValidAsync()
    {
        await _documentAsserter.HasValidStructureAsync(DocumentType.NotifyWholesaleServices, "3", _skipIdentificationLengthValidation).ConfigureAwait(false);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument SettlementMethodDoesNotExist()
    {
        _documentAsserter.IsNotPresent("PayloadEnergyTimeSeries[1]/DetailMeasurementMeteringPointCharacteristic/SettlementMethod");
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasQualityForPosition(
        int expectedPosition,
        CalculatedQuantityQuality expectedQuantityQuality)
    {
        _documentAsserter
            .HasValue(
                $"{PayloadEnergyTimeSeries}[1]/IntervalEnergyObservation[{expectedPosition}]/Position",
                expectedPosition.ToString(CultureInfo.InvariantCulture))
            .HasValue(
                $"{PayloadEnergyTimeSeries}[1]/IntervalEnergyObservation[{expectedPosition}]/QuantityQuality",
                EbixCode.ForWholesaleServicesOf(expectedQuantityQuality)!);

        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasAnyPoints()
    {
        _documentAsserter.ElementExists($"{PayloadEnergyTimeSeries}[1]/IntervalEnergyObservation[1]");

        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasPoints(IReadOnlyCollection<WholesaleServicesRequestSeries.Types.Point> points)
    {
        throw new NotImplementedException();
    }

    public IAssertNotifyWholesaleServicesDocument HasSettlementVersion(SettlementVersion expectedSettlementVersion)
    {
        _documentAsserter.HasValue("ProcessEnergyContext/ProcessVariant", EbixCode.Of(expectedSettlementVersion));
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument SettlementVersionDoesNotExist()
    {
        _documentAsserter.IsNotPresent("ProcessEnergyContext/ProcessVariant");
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasOriginalTransactionIdReference(string expectedOriginalTransactionIdReference)
    {
        _documentAsserter.HasValue(
            $"{PayloadEnergyTimeSeries}[1]/OriginalBusinessDocument",
            expectedOriginalTransactionIdReference);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument OriginalTransactionIdReferenceDoesNotExist()
    {
        _documentAsserter.IsNotPresent($"{PayloadEnergyTimeSeries}[1]/OriginalBusinessDocument");
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasSettlementMethod(SettlementMethod expectedSettlementMethod)
    {
        _documentAsserter.HasValue($"{PayloadEnergyTimeSeries}[1]/DetailMeasurementMeteringPointCharacteristic/SettlementMethod", expectedSettlementMethod.Code);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasPriceForPosition(int position, string? expectedPrice)
    {
        _documentAsserter.HasValue($"{PayloadEnergyTimeSeries}[1]/IntervalEnergyObservation[{position}]/EnergyPrice", expectedPrice ?? "0");
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
