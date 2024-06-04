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
using System.Xml.XPath;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats.Ebix;
using Energinet.DataHub.Edi.Responses;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.Asserts;
using FluentAssertions;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;
using Resolution = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Resolution;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.NotifyAggregatedMeasureData;

public class AssertNotifyAggregatedMeasureDataEbixDocument : IAssertNotifyAggregatedMeasureDataDocument
{
    private readonly AssertEbixDocument _documentAsserter;
    private readonly bool _skipIdentificationLengthValidation;

    public AssertNotifyAggregatedMeasureDataEbixDocument(AssertEbixDocument documentAsserter, bool skipIdentificationLengthValidation = false)
    {
        _documentAsserter = documentAsserter;
        _skipIdentificationLengthValidation = skipIdentificationLengthValidation;
        _documentAsserter.HasValue("HeaderEnergyDocument/DocumentType", "E31");
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasMessageId(string expectedMessageId)
    {
        _documentAsserter.HasValue("HeaderEnergyDocument/Identification", expectedMessageId);
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument MessageIdExists()
    {
        _documentAsserter.ElementExists("HeaderEnergyDocument/Identification");
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasSenderId(string expectedSenderId)
    {
        _documentAsserter.HasValue("HeaderEnergyDocument/SenderEnergyParty/Identification", expectedSenderId);
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasReceiverId(string expectedReceiverId)
    {
        _documentAsserter.HasValue("HeaderEnergyDocument/RecipientEnergyParty/Identification", expectedReceiverId);
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasTimestamp(string expectedTimestamp)
    {
        _documentAsserter.HasValue("HeaderEnergyDocument/Creation", expectedTimestamp);
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasTransactionId(TransactionId expectedTransactionId)
    {
        ArgumentNullException.ThrowIfNull(expectedTransactionId);
        _documentAsserter.HasValue("PayloadEnergyTimeSeries[1]/Identification", expectedTransactionId.Value);
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument TransactionIdExists()
    {
        _documentAsserter.ElementExists("PayloadEnergyTimeSeries[1]/Identification");
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasGridAreaCode(string expectedGridAreaCode)
    {
        _documentAsserter.HasValue("PayloadEnergyTimeSeries[1]/MeteringGridAreaUsedDomainLocation/Identification", expectedGridAreaCode);
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasBalanceResponsibleNumber(string expectedBalanceResponsibleNumber)
    {
        _documentAsserter.HasValue("PayloadEnergyTimeSeries[1]/BalanceResponsibleEnergyParty/Identification", expectedBalanceResponsibleNumber);
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasEnergySupplierNumber(string expectedEnergySupplierNumber)
    {
        _documentAsserter.HasValue("PayloadEnergyTimeSeries[1]/BalanceSupplierEnergyParty/Identification", expectedEnergySupplierNumber);
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasProductCode(string expectedProductCode)
    {
        _documentAsserter.HasValue("PayloadEnergyTimeSeries[1]/IncludedProductCharacteristic/Identification", expectedProductCode);
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasPeriod(Period expectedPeriod)
    {
        ArgumentNullException.ThrowIfNull(expectedPeriod);
        _documentAsserter
            .HasValue("PayloadEnergyTimeSeries[1]/ObservationTimeSeriesPeriod/Start", expectedPeriod.StartToEbixString())
            .HasValue("PayloadEnergyTimeSeries[1]/ObservationTimeSeriesPeriod/End", expectedPeriod.EndToEbixString());
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasPoint(int position, int quantity)
    {
        _documentAsserter
            .HasValue("PayloadEnergyTimeSeries[1]/IntervalEnergyObservation[1]/Position", position.ToString(CultureInfo.InvariantCulture))
            .HasValue("PayloadEnergyTimeSeries[1]/IntervalEnergyObservation[1]/EnergyQuantity", quantity.ToString(CultureInfo.InvariantCulture));
        return this;
    }

    public async Task<IAssertNotifyAggregatedMeasureDataDocument> DocumentIsValidAsync()
    {
        await _documentAsserter.HasValidStructureAsync(DocumentType.NotifyAggregatedMeasureData, "3", _skipIdentificationLengthValidation).ConfigureAwait(false);
        return this;
    }

    public async Task<IAssertNotifyAggregatedMeasureDataDocument> HasStructureValidationErrorsAsync(
        IReadOnlyCollection<string> errors)
    {
        var actualErrors = await _documentAsserter
            .HasStructureValidationErrorsAsync(DocumentType.NotifyAggregatedMeasureData, "3")
            .ConfigureAwait(false);

        actualErrors.Should()
            .SatisfyRespectively(errors.Select<string, Action<string>>(e => ae => ae.Should().Contain(e)).ToList());

        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument SettlementMethodIsNotPresent()
    {
        _documentAsserter.IsNotPresent("PayloadEnergyTimeSeries[1]/DetailMeasurementMeteringPointCharacteristic/SettlementMethod");
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument EnergySupplierNumberIsNotPresent()
    {
        _documentAsserter.IsNotPresent("PayloadEnergyTimeSeries[1]/BalanceSupplierEnergyParty/Identification");
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument BalanceResponsibleNumberIsNotPresent()
    {
        _documentAsserter.IsNotPresent("PayloadEnergyTimeSeries[1]/BalanceResponsibleEnergyParty/Identification");
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument QuantityIsNotPresentForPosition(int position)
    {
        _documentAsserter.IsNotPresent($"PayloadEnergyTimeSeries[1]/IntervalEnergyObservation[{position}]/EnergyQuantity");
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument QualityIsNotPresentForPosition(int position)
    {
        _documentAsserter.IsNotPresent($"PayloadEnergyTimeSeries[1]/IntervalEnergyObservation[{position}]/QuantityQuality");
        _documentAsserter.HasValue($"PayloadEnergyTimeSeries[1]/IntervalEnergyObservation[{position}]/QuantityMissing", "true");
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument QualityIsPresentForPosition(
        int position,
        string quantityQualityCode)
    {
        _documentAsserter.HasValue(
            $"PayloadEnergyTimeSeries[1]/IntervalEnergyObservation[{position}]/QuantityQuality",
            quantityQualityCode);

        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasCalculationResultVersion(long version)
    {
        _documentAsserter.HasValue("PayloadEnergyTimeSeries[1]/Version", version.ToString(NumberFormatInfo.InvariantInfo));
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasMeteringPointType(MeteringPointType meteringPointType)
    {
        _documentAsserter.HasValue($"PayloadEnergyTimeSeries[1]/DetailMeasurementMeteringPointCharacteristic/TypeOfMeteringPoint", EbixCode.Of(meteringPointType));
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasQuantityMeasurementUnit(MeasurementUnit quantityMeasurementUnit)
    {
        _documentAsserter.HasValueWithAttributes(
            $"PayloadEnergyTimeSeries[1]/IncludedProductCharacteristic/UnitType",
            EbixCode.Of(quantityMeasurementUnit));
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasResolution(Resolution resolution)
    {
        ArgumentNullException.ThrowIfNull(resolution);
        _documentAsserter.HasValue(
            "PayloadEnergyTimeSeries[1]/ObservationTimeSeriesPeriod/ResolutionDuration",
            EbixCode.Of(resolution));
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasPoints(IReadOnlyCollection<TimeSeriesPoint> points)
    {
        var pointsInDocument = _documentAsserter
            .GetElements($"PayloadEnergyTimeSeries[1]/IntervalEnergyObservation")!;

        pointsInDocument.Should().HaveSameCount(points);

        var expectedPoints = points.OrderBy(p => p.Time).ToList();

        for (var i = 0; i < pointsInDocument.Count; i++)
        {
            pointsInDocument[i]
                .XPathSelectElement(_documentAsserter.EnsureXPathHasPrefix("Position"), _documentAsserter.XmlNamespaceManager)!
                .Value
                .ToInt()
                .Should()
                .Be(i + 1);

            pointsInDocument[i]
                .XPathSelectElement(_documentAsserter.EnsureXPathHasPrefix("EnergyQuantity"), _documentAsserter.XmlNamespaceManager)!
                .Value
                .ToDecimal()
                .Should()
                .Be(expectedPoints[i].Quantity.ToDecimal());

            var expectedQuantityQuality = expectedPoints[i].QuantityQualities.Single() switch
            {
                QuantityQuality.Calculated => EbixCode.QuantityQualityCodeCalculated,
                QuantityQuality.Estimated => EbixCode.QuantityQualityCodeEstimated,
                QuantityQuality.Measured => EbixCode.QuantityQualityCodeMeasured,
                _ => throw new NotImplementedException(
                    $"Quantity quality {expectedPoints[i].QuantityQualities.Single()} not implemented"),
            };

            pointsInDocument[i]
                .XPathSelectElement(_documentAsserter.EnsureXPathHasPrefix("QuantityQuality"), _documentAsserter.XmlNamespaceManager)!
                .Value
                .Should()
                .Be(expectedQuantityQuality);
        }

        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasBusinessReason(BusinessReason businessReason)
    {
        _documentAsserter.HasValue("ProcessEnergyContext/EnergyBusinessProcess", EbixCode.Of(businessReason));
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasSettlementVersion(SettlementVersion settlementVersion)
    {
        _documentAsserter.HasValue("ProcessEnergyContext/ProcessVariant", EbixCode.Of(settlementVersion));
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument SettlementVersionIsNotPresent()
    {
        _documentAsserter.IsNotPresent("ProcessEnergyContext/ProcessVariant");
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasOriginalTransactionIdReference(
        TransactionId originalTransactionIdReference)
    {
        ArgumentNullException.ThrowIfNull(originalTransactionIdReference);
        _documentAsserter.HasValue(
            "PayloadEnergyTimeSeries[1]/OriginalBusinessDocument",
            originalTransactionIdReference.Value);
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument OriginalTransactionIdReferenceDoesNotExist()
    {
        _documentAsserter.IsNotPresent($"PayloadEnergyTimeSeries[1]/OriginalBusinessDocument");
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasSettlementMethod(SettlementMethod settlementMethod)
    {
        _documentAsserter.HasValue("PayloadEnergyTimeSeries[1]/DetailMeasurementMeteringPointCharacteristic/SettlementMethod", EbixCode.Of(settlementMethod));
        return this;
    }
}
