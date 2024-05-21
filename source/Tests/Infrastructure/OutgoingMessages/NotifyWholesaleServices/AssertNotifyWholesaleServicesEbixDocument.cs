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
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats.Ebix;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Energinet.DataHub.Edi.Responses;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.Asserts;
using FluentAssertions;
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

    public AssertNotifyWholesaleServicesEbixDocument(
        AssertEbixDocument documentAsserter,
        bool skipIdentificationLengthValidation = false)
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

    public IAssertNotifyWholesaleServicesDocument HasReceiverRole(
        ActorRole expectedReceiverRole,
        CodeListType codeListType)
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

    public IAssertNotifyWholesaleServicesDocument HasTransactionId(TransactionId expectedTransactionId)
    {
        ArgumentNullException.ThrowIfNull(expectedTransactionId);
        _documentAsserter.HasValue($"{PayloadEnergyTimeSeries}[1]/Identification", expectedTransactionId.Value);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument TransactionIdExists()
    {
        _documentAsserter.ElementExists($"{PayloadEnergyTimeSeries}[1]/Identification");
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasCalculationVersion(long expectedVersion)
    {
        _documentAsserter.HasValue(
            $"{PayloadEnergyTimeSeries}[1]/Version",
            expectedVersion.ToString(CultureInfo.InvariantCulture));
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasChargeTypeOwner(
        ActorNumber expectedChargeTypeOwner,
        string codingScheme)
    {
        ArgumentNullException.ThrowIfNull(expectedChargeTypeOwner);
        _documentAsserter.HasValueWithAttributes(
            $"{PayloadEnergyTimeSeries}[1]/ChargeTypeOwnerEnergyParty/Identification",
            expectedChargeTypeOwner.Value,
            CreateRequiredSchemeAttribute(expectedChargeTypeOwner));
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument ChargeTypeOwnerDoesNotExist()
    {
        _documentAsserter.IsNotPresent($"{PayloadEnergyTimeSeries}[1]/ChargeTypeOwnerEnergyParty/Identification");
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasMeteringPointType(MeteringPointType expectedMeteringPointType)
    {
        ArgumentNullException.ThrowIfNull(expectedMeteringPointType);
        _documentAsserter.HasValue(
            $"{PayloadEnergyTimeSeries}[1]/DetailMeasurementMeteringPointCharacteristic/TypeOfMeteringPoint",
            expectedMeteringPointType.Code);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument MeteringPointTypeDoesNotExist()
    {
        _documentAsserter.IsNotPresent(
            $"{PayloadEnergyTimeSeries}[1]/DetailMeasurementMeteringPointCharacteristic/TypeOfMeteringPoint");
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasChargeCode(string expectedChargeTypeNumber)
    {
        _documentAsserter.HasValue($"{PayloadEnergyTimeSeries}[1]/PartyChargeTypeID", expectedChargeTypeNumber);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument ChargeCodeDoesNotExist()
    {
        _documentAsserter.IsNotPresent($"{PayloadEnergyTimeSeries}[1]/PartyChargeTypeID");
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasChargeType(ChargeType expectedChargeType)
    {
        ArgumentNullException.ThrowIfNull(expectedChargeType);
        _documentAsserter.HasValueWithAttributes(
            $"{PayloadEnergyTimeSeries}[1]/ChargeType",
            EbixCode.Of(expectedChargeType),
            CreateRequiredListAttributes(CodeListType.EbixDenmark));
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument ChargeTypeDoesNotExist()
    {
        _documentAsserter.IsNotPresent($"{PayloadEnergyTimeSeries}[1]/ChargeType");
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

    public IAssertNotifyWholesaleServicesDocument PriceMeasurementUnitDoesNotExist()
    {
        // Is not used in ebIX document
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
            .HasValue(
                $"{PayloadEnergyTimeSeries}[1]/ObservationTimeSeriesPeriod/Start",
                expectedPeriod.StartToEbixString())
            .HasValue(
                $"{PayloadEnergyTimeSeries}[1]/ObservationTimeSeriesPeriod/End",
                expectedPeriod.EndToEbixString());
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasResolution(Resolution resolution)
    {
        ArgumentNullException.ThrowIfNull(resolution);
        _documentAsserter.HasValue(
            $"{PayloadEnergyTimeSeries}[1]/ObservationTimeSeriesPeriod/ResolutionDuration",
            EbixCode.Of(resolution));
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument ResolutionDoesNotExist()
    {
        _documentAsserter.IsNotPresent(
            $"{PayloadEnergyTimeSeries}[1]/ObservationTimeSeriesPeriod/ResolutionDuration");
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasSumQuantityForPosition(
        int expectedPosition,
        int expectedSumQuantity)
    {
        _documentAsserter
            .HasValue(
                $"{PayloadEnergyTimeSeries}[1]/IntervalEnergyObservation[{expectedPosition}]/Position",
                expectedPosition.ToString(CultureInfo.InvariantCulture))
            .HasValue(
                $"{PayloadEnergyTimeSeries}[1]/IntervalEnergyObservation[{expectedPosition}]/EnergySum",
                expectedSumQuantity.ToString(CultureInfo.InvariantCulture));
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasQuantityForPosition(int expectedPosition, int expectedQuantity)
    {
        _documentAsserter
            .HasValue(
                $"{PayloadEnergyTimeSeries}[1]/IntervalEnergyObservation[{expectedPosition}]/Position",
                expectedPosition.ToString(CultureInfo.InvariantCulture))
            .HasValue(
                $"{PayloadEnergyTimeSeries}[1]/IntervalEnergyObservation[{expectedPosition}]/EnergyQuantity",
                expectedQuantity.ToString(CultureInfo.InvariantCulture));
        return this;
    }

    public async Task<IAssertNotifyWholesaleServicesDocument> DocumentIsValidAsync()
    {
        await _documentAsserter.HasValidStructureAsync(
                DocumentType.NotifyWholesaleServices,
                "3",
                _skipIdentificationLengthValidation)
            .ConfigureAwait(false);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument SettlementMethodDoesNotExist()
    {
        _documentAsserter.IsNotPresent(
            "PayloadEnergyTimeSeries[1]/DetailMeasurementMeteringPointCharacteristic/SettlementMethod");
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

    public IAssertNotifyWholesaleServicesDocument HasPoints(
        IReadOnlyCollection<WholesaleServicesRequestSeries.Types.Point> points)
    {
        var pointsInDocument = _documentAsserter
            .GetElements($"{PayloadEnergyTimeSeries}[1]/IntervalEnergyObservation")!;

        pointsInDocument.Should().HaveSameCount(points);

        var expectedPoints = points.OrderBy(p => p.Time).ToList();

        for (var i = 0; i < pointsInDocument.Count; i++)
        {
            AssertEnergySum(pointsInDocument, i, expectedPoints[i].Amount.ToDecimal());

            AssertQuantity(pointsInDocument, i, expectedPoints[i].Quantity.ToDecimal());

            AssertPosition(pointsInDocument, i);

            AssertPrice(pointsInDocument, i, expectedPoints[i].Price.ToDecimal());

            AssertQuantityQuality(pointsInDocument, i, expectedPoints[i].QuantityQualities.Single());
        }

        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasPoints(
        IReadOnlyCollection<WholesaleServicesPoint> points)
    {
        var pointsInDocument = _documentAsserter
            .GetElements($"{PayloadEnergyTimeSeries}[1]/IntervalEnergyObservation")!;

        pointsInDocument.Should().HaveSameCount(points);

        var expectedPoints = points.OrderBy(p => p.Position).ToList();

        for (var i = 0; i < pointsInDocument.Count; i++)
        {
            AssertEnergySum(pointsInDocument, i, expectedPoints[i].Amount);

            AssertQuantity(pointsInDocument, i, expectedPoints[i].Quantity);

            AssertPosition(pointsInDocument, i);

            AssertPrice(pointsInDocument, i, expectedPoints[i].Price);

            AssertQuantityQuality(pointsInDocument, i, expectedPoints[i].QuantityQuality);
        }

        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasSinglePointWithAmount(DecimalValue expectedAmount)
    {
        var pointsInDocument = _documentAsserter
            .GetElements($"{PayloadEnergyTimeSeries}[1]/IntervalEnergyObservation")!;

        var pointInDocument = pointsInDocument.Single();

        pointInDocument
            .XPathSelectElement(
                _documentAsserter.EnsureXPathHasPrefix("EnergySum"),
                _documentAsserter.XmlNamespaceManager)!
            .Value
            .ToDecimal()
            .Should()
            .Be(expectedAmount.ToDecimal());

        pointInDocument
            .XPathSelectElement(
                _documentAsserter.EnsureXPathHasPrefix("Position"),
                _documentAsserter.XmlNamespaceManager)!
            .Value
            .ToInt()
            .Should()
            .Be(1);

        _documentAsserter.IsNotPresent($"PayloadEnergyTimeSeries[1]/IntervalEnergyObservation[1]/EnergyQuantity");

        _documentAsserter.IsNotPresent("PayloadEnergyTimeSeries[1]/IntervalEnergyObservation[1]/EnergyPrice");

        _documentAsserter.IsNotPresent($"PayloadEnergyTimeSeries[1]/IntervalEnergyObservation[1]/QuantityQuality");

        return this;
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

    public IAssertNotifyWholesaleServicesDocument HasOriginalTransactionIdReference(
        TransactionId expectedOriginalTransactionIdReference)
    {
        ArgumentNullException.ThrowIfNull(expectedOriginalTransactionIdReference);
        _documentAsserter.HasValue(
            $"{PayloadEnergyTimeSeries}[1]/OriginalBusinessDocument",
            expectedOriginalTransactionIdReference.Value);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument OriginalTransactionIdReferenceDoesNotExist()
    {
        _documentAsserter.IsNotPresent($"{PayloadEnergyTimeSeries}[1]/OriginalBusinessDocument");
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasSettlementMethod(SettlementMethod expectedSettlementMethod)
    {
        ArgumentNullException.ThrowIfNull(expectedSettlementMethod);
        _documentAsserter.HasValue(
            $"{PayloadEnergyTimeSeries}[1]/DetailMeasurementMeteringPointCharacteristic/SettlementMethod",
            expectedSettlementMethod.Code);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasPriceForPosition(int position, string? expectedPrice)
    {
        _documentAsserter.HasValue(
            $"{PayloadEnergyTimeSeries}[1]/IntervalEnergyObservation[{position}]/EnergyPrice",
            expectedPrice ?? "0");
        return this;
    }

    #endregion

    private static AttributeNameAndValue[] CreateRequiredListAttributes(CodeListType codeListType)
    {
        var (codeList, countryCode) = GetCodeListConstant(codeListType);

        var requiredAttributes = new List<AttributeNameAndValue> { new("listAgencyIdentifier", codeList), };

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
        var codeOwner = GetActorNumberOwner(
            ActorNumber.IsGlnNumber(actorNumber.Value) ? ActorNumberType.Gln : ActorNumberType.Eic);
        var requiredAttribute = new AttributeNameAndValue("schemeAgencyIdentifier", codeOwner);

        return requiredAttribute;
    }

    private static string GetActorNumberOwner(ActorNumberType actorNumberType) => actorNumberType switch
    {
        ActorNumberType.Gln => EbixDocumentWriter.Gs1Code,
        ActorNumberType.Eic => EbixDocumentWriter.EicCode,
        _ => throw new ArgumentOutOfRangeException(nameof(actorNumberType), actorNumberType, "Invalid ActorNumberType"),
    };

    private void AssertEnergySum(IList<XElement> pointsInDocument, int i, decimal? expectedAmount)
    {
        pointsInDocument[i]
            .XPathSelectElement(
                _documentAsserter.EnsureXPathHasPrefix("EnergySum"),
                _documentAsserter.XmlNamespaceManager)!
            .Value
            .ToDecimal()
            .Should()
            .Be(expectedAmount);
    }

    private void AssertQuantity(IList<XElement> pointsInDocument, int i, decimal? expectedQuantity)
    {
        pointsInDocument[i]
            .XPathSelectElement(
                _documentAsserter.EnsureXPathHasPrefix("EnergyQuantity"),
                _documentAsserter.XmlNamespaceManager)!
            .Value
            .ToDecimal()
            .Should()
            .Be(expectedQuantity);
    }

    private void AssertPosition(IList<XElement> pointsInDocument, int i)
    {
        pointsInDocument[i]
            .XPathSelectElement(
                _documentAsserter.EnsureXPathHasPrefix("Position"),
                _documentAsserter.XmlNamespaceManager)!
            .Value
            .ToInt()
            .Should()
            .Be(i + 1);
    }

    private void AssertPrice(IList<XElement> pointsInDocument, int i, decimal? expectedPrice)
    {
        pointsInDocument[i]
            .XPathSelectElement(
                _documentAsserter.EnsureXPathHasPrefix("EnergyPrice"),
                _documentAsserter.XmlNamespaceManager)!
            .Value
            .ToDecimal()
            .Should()
            .Be(expectedPrice);
    }

    private void AssertQuantityQuality(
        IList<XElement> pointsInDocument,
        int i,
        CalculatedQuantityQuality? expectedQuantityQuality)
    {
        if (expectedQuantityQuality != null)
        {
            var translatedQuantityQuality = expectedQuantityQuality switch
            {
                // For WholesaleServices then calculated, estimated and measured is written as calculated
                CalculatedQuantityQuality.Missing => null,
                CalculatedQuantityQuality.NotAvailable => null,
                CalculatedQuantityQuality.Incomplete => EbixCode.QuantityQualityCodeCalculated,
                CalculatedQuantityQuality.Calculated => EbixCode.QuantityQualityCodeCalculated,
                CalculatedQuantityQuality.Estimated => EbixCode.QuantityQualityCodeCalculated,
                CalculatedQuantityQuality.Measured => EbixCode.QuantityQualityCodeCalculated,
                _ => throw new NotImplementedException(
                    $"Quantity quality {expectedQuantityQuality} not implemented"),
            };

            if (translatedQuantityQuality != null)
            {
                pointsInDocument[i]
                    .XPathSelectElement(
                        _documentAsserter.EnsureXPathHasPrefix("QuantityQuality"),
                        _documentAsserter.XmlNamespaceManager)!
                    .Value
                    .Should()
                    .Be(translatedQuantityQuality);
            }
            else
            {
                _documentAsserter.IsNotPresent($"Series[1]/Period/Point[{i + 1}]/QuantityQuality");
            }
        }
        else
        {
            _documentAsserter.IsNotPresent($"Series[1]/Period/Point[{i + 1}]/QuantityQuality");
        }
    }

    private void AssertQuantityQuality(
        IList<XElement> pointsInDocument,
        int i,
        QuantityQuality? expectedQuantityQuality)
    {
        if (expectedQuantityQuality != null)
        {
            var translatedQuantityQuality = expectedQuantityQuality switch
            {
                // For WholesaleServices then calculated, estimated and measured is written as calculated
                QuantityQuality.Missing => null,
                QuantityQuality.Calculated => EbixCode.QuantityQualityCodeCalculated,
                QuantityQuality.Estimated => EbixCode.QuantityQualityCodeCalculated,
                QuantityQuality.Measured => EbixCode.QuantityQualityCodeCalculated,
                _ => throw new NotImplementedException(
                    $"Quantity quality {expectedQuantityQuality} not implemented"),
            };

            if (translatedQuantityQuality != null)
            {
                pointsInDocument[i]
                    .XPathSelectElement(
                        _documentAsserter.EnsureXPathHasPrefix("QuantityQuality"),
                        _documentAsserter.XmlNamespaceManager)!
                    .Value
                    .Should()
                    .Be(translatedQuantityQuality);
            }
            else
            {
                _documentAsserter.IsNotPresent($"Series[1]/Period/Point[{i + 1}]/QuantityQuality");
            }
        }
        else
        {
            _documentAsserter.IsNotPresent($"Series[1]/Period/Point[{i + 1}]/QuantityQuality");
        }
    }
}
