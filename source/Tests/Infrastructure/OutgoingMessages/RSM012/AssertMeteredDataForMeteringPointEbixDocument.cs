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
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats.Ebix;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.Asserts;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.NotifyWholesaleServices;
using FluentAssertions;
using NodaTime;
using NodaTime.Text;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.RSM012;

public class AssertMeteredDataForMeteringPointEbixDocument : IAssertMeteredDataForMeteringPointDocumentDocument
{
    private readonly AssertEbixDocument _documentAsserter;

    public AssertMeteredDataForMeteringPointEbixDocument(
        AssertEbixDocument documentAsserter,
        bool skipIdentificationLengthValdation = false)
    {
        _documentAsserter = documentAsserter;
        _documentAsserter.HasValue("HeaderEnergyDocument/DocumentType", "E66");
    }

    public IAssertMeteredDataForMeteringPointDocumentDocument HasBusinessReason(string expectedBusinessReasonCode)
    {
        _documentAsserter.HasValueWithAttributes(
            "ProcessEnergyContext/EnergyBusinessProcess",
            expectedBusinessReasonCode,
            CreateRequiredListAttributes(expectedBusinessReasonCode.StartsWith('D') ? CodeListType.EbixDenmark : CodeListType.Ebix));
        return this;
    }

    public IAssertMeteredDataForMeteringPointDocumentDocument HasBusinessSectorType(string? expectedBusinessSectorType)
    {
        if (expectedBusinessSectorType is null)
        {
            _documentAsserter.IsNotPresent("ProcessEnergyContext/EnergyIndustryClassification");
        }
        else
        {
            _documentAsserter.HasValue(
                "ProcessEnergyContext/EnergyIndustryClassification",
                expectedBusinessSectorType);
        }

        return this;
    }

    public IAssertMeteredDataForMeteringPointDocumentDocument HasEndedDateTime(int seriesIndex, string expectedEndedDateTime)
    {
        if (expectedEndedDateTime.Count(c => c == ':') == 1)
        {
            expectedEndedDateTime = expectedEndedDateTime.Insert(expectedEndedDateTime.Length - 1, ":00");
        }

        _documentAsserter.HasValue(
            $"PayloadEnergyTimeSeries[{seriesIndex}]/ObservationTimeSeriesPeriod/End",
            InstantPattern.General.Parse(expectedEndedDateTime).Value.ToString("yyyy-MM-ddTHH:mm:ss'Z'", CultureInfo.InvariantCulture));
        return this;
    }

    // Not sure how this looks in ebix?
    public IAssertMeteredDataForMeteringPointDocumentDocument HasInDomain(int seriesIndex, string? expectedInDomain)
    {
        if (expectedInDomain is null)
        {
            _documentAsserter.IsNotPresent($"PayloadEnergyTimeSeries[{seriesIndex}]/InDomain");
            return this;
        }

        _documentAsserter.HasValue($"PayloadEnergyTimeSeries[{seriesIndex}]/InDomain", expectedInDomain);
        return this;
    }

    public IAssertMeteredDataForMeteringPointDocumentDocument HasMeteringPointNumber(int seriesIndex, string expectedMeteringPointNumber, string expectedSchemeCode)
    {
        _documentAsserter.HasValue($"PayloadEnergyTimeSeries[{seriesIndex}]/MeteringPointDomainLocation/Identification", expectedMeteringPointNumber);
        return this;
    }

    public IAssertMeteredDataForMeteringPointDocumentDocument HasMeteringPointType(int seriesIndex, MeteringPointType expectedMeteringPointType)
    {
        _documentAsserter.HasValue($"PayloadEnergyTimeSeries[{seriesIndex}]/DetailMeasurementMeteringPointCharacteristic/TypeOfMeteringPoint", expectedMeteringPointType.Code);
        return this;
    }

    // OriginalTransactionIdReferenceId not present in ebix.
    public IAssertMeteredDataForMeteringPointDocumentDocument HasOriginalTransactionIdReferenceId(int seriesIndex, string? expectedOriginalTransactionIdReferenceId)
    {
        return this;
    }

    // Not sure how this looks in ebix?
    public IAssertMeteredDataForMeteringPointDocumentDocument HasOutDomain(int seriesIndex, string? expectedOutDomain)
    {
        if (expectedOutDomain is null)
        {
            _documentAsserter.IsNotPresent($"PayloadEnergyTimeSeries[{seriesIndex}]/OutDomain");
            return this;
        }

        _documentAsserter.HasValue($"PayloadEnergyTimeSeries[{seriesIndex}]/OutDomain", expectedOutDomain);
        return this;
    }

    public IAssertMeteredDataForMeteringPointDocumentDocument HasPoints(int seriesIndex, IReadOnlyList<AssertPointDocumentFieldsInput> expectedPoints)
    {
        var pointsInDocument = _documentAsserter
            .GetElements($"PayloadEnergyTimeSeries[{seriesIndex}]/IntervalEnergyObservation");

        pointsInDocument.Should().HaveSameCount(expectedPoints);

        for (var i = 0; i < expectedPoints.Count; i++)
        {
            var (requiredPointDocumentFields, optionalPointDocumentFields) = expectedPoints[i];

            _documentAsserter
                .HasValue(
                    $"PayloadEnergyTimeSeries[{seriesIndex}]/IntervalEnergyObservation[{i + 1}]/Position",
                    requiredPointDocumentFields.Position.ToString());

            if (optionalPointDocumentFields.Quantity.HasValue)
            {
                _documentAsserter
                    .HasValue(
                        $"PayloadEnergyTimeSeries[{seriesIndex}]/IntervalEnergyObservation[{i + 1}]/EnergyQuantity",
                        optionalPointDocumentFields.Quantity.Value.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                AssertElementNotPresent($"PayloadEnergyTimeSeries[{seriesIndex}]/IntervalEnergyObservation[{i + 1}]/EnergyQuantity");
            }

            if (optionalPointDocumentFields.Quality != null)
            {
                var quality = EbixCode.Of(optionalPointDocumentFields.Quality);

                if (quality is not null)
                {
                    _documentAsserter
                    .HasValue(
                        $"PayloadEnergyTimeSeries[{seriesIndex}]/IntervalEnergyObservation[{i + 1}]/QuantityQuality",
                        quality);
                }
            }
            else
            {
                AssertElementNotPresent($"PayloadEnergyTimeSeries[{seriesIndex}]/IntervalEnergyObservation[{i + 1}]/QuantityQuality");
            }
        }

        return this;

        void AssertElementNotPresent(string xpath)
        {
            _documentAsserter.IsNotPresent(xpath);
        }
    }

    // Not sure how this looks in ebix?
    public IAssertMeteredDataForMeteringPointDocumentDocument HasProduct(int seriesIndex, string? expectedProduct)
    {
        if (expectedProduct is null)
        {
            _documentAsserter.IsNotPresent($"PayloadEnergyTimeSeries[{seriesIndex}]/IncludedProductCharacteristic/Identification");
            return this;
        }

        _documentAsserter.HasValue($"PayloadEnergyTimeSeries[{seriesIndex}]/IncludedProductCharacteristic/Identification", expectedProduct);
        return this;
    }

    // Not sure how this looks in ebix?
    public IAssertMeteredDataForMeteringPointDocumentDocument HasQuantityMeasureUnit(int seriesIndex, string expectedQuantityMeasureUnit)
    {
        _documentAsserter.IsNotPresent($"PayloadEnergyTimeSeries[{seriesIndex}]/QuantityMeasureUnit");
        return this;
    }

    // Not sure how this looks in ebix?
    public IAssertMeteredDataForMeteringPointDocumentDocument HasReceiverId(string expectedReceiverId, string expectedSchemeCode)
    {
        _documentAsserter.IsNotPresent($"Receiver");
        return this;
    }

    // Not sure how this looks in ebix?
    public IAssertMeteredDataForMeteringPointDocumentDocument HasReceiverRole(string expectedReceiverRole)
    {
        _documentAsserter.IsNotPresent($"ReceiverRole");
        return this;
    }

    // Not sure how this looks in ebix?
    public IAssertMeteredDataForMeteringPointDocumentDocument HasRegistrationDateTime(int seriesIndex, string? expectedRegistrationDateTime)
    {
        return this;
    }

    public IAssertMeteredDataForMeteringPointDocumentDocument HasResolution(int seriesIndex, string expectedResolution)
    {
        _documentAsserter.HasValue($"PayloadEnergyTimeSeries[{seriesIndex}]/ObservationTimeSeriesPeriod/ResolutionDuration", expectedResolution);
        return this;
    }

    // Not sure how this looks in ebix?
    public IAssertMeteredDataForMeteringPointDocumentDocument HasSenderId(string expectedSenderId, string expectedSchemeCode)
    {
        _documentAsserter.IsNotPresent($"SenderId");
        return this;
    }

    // Not sure how this looks in ebix?
    public IAssertMeteredDataForMeteringPointDocumentDocument HasSenderRole(string expectedSenderRole)
    {
        _documentAsserter.IsNotPresent($"SenderRole");
        return this;
    }

    public IAssertMeteredDataForMeteringPointDocumentDocument HasStartedDateTime(int seriesIndex, string expectedStartedDateTime)
    {
        if (expectedStartedDateTime.Count(c => c == ':') == 1)
        {
            expectedStartedDateTime = expectedStartedDateTime.Insert(expectedStartedDateTime.Length - 1, ":00");
        }

        _documentAsserter.HasValue(
            $"PayloadEnergyTimeSeries[{seriesIndex}]/ObservationTimeSeriesPeriod/Start",
            InstantPattern.General.Parse(expectedStartedDateTime).Value.ToString("yyyy-MM-ddTHH:mm:ss'Z'", CultureInfo.InvariantCulture));
        return this;
    }

    public IAssertMeteredDataForMeteringPointDocumentDocument HasTimestamp(string expectedTimestamp)
    {
        _documentAsserter.HasValue($"HeaderEnergyDocument/Creation", expectedTimestamp);
        return this;
    }

    public IAssertMeteredDataForMeteringPointDocumentDocument HasTransactionId(int seriesIndex, TransactionId expectedTransactionId)
    {
        _documentAsserter.HasValue($"PayloadEnergyTimeSeries[{seriesIndex}]/Identification", expectedTransactionId.Value);
        return this;
    }

    public IAssertMeteredDataForMeteringPointDocumentDocument MessageIdExists()
    {
        _documentAsserter.ElementExists("HeaderEnergyDocument/Identification");
        return this;
    }

    public IAssertMeteredDataForMeteringPointDocumentDocument TransactionIdExists(int seriesIndex)
    {
        _documentAsserter.ElementExists($"PayloadEnergyTimeSeries[{seriesIndex}]/Identification");
        return this;
    }

    public async Task<IAssertMeteredDataForMeteringPointDocumentDocument> DocumentIsValidAsync()
    {
        await _documentAsserter.HasValidStructureAsync(DocumentType.NotifyValidatedMeasureData, "3").ConfigureAwait(false);
        return this;
    }

    private static AttributeNameAndValue[] CreateRequiredListAttributes(CodeListType codeListType)
    {
        var (codeList, countryCode) = GetCodeListConstant(codeListType);

        var requiredAttributes = new List<AttributeNameAndValue> { new("listAgencyIdentifier", codeList), };

        if (!string.IsNullOrEmpty(countryCode))
            requiredAttributes.Add(new("listIdentifier", countryCode));

        return [.. requiredAttributes];
    }

    private static (string CodeList, string? CountryCode) GetCodeListConstant(CodeListType codeListType) =>
        codeListType switch
        {
            CodeListType.UnitedNations => (EbixDocumentWriter.UnitedNationsCodeList, null),
            CodeListType.Ebix => (EbixDocumentWriter.EbixCodeList, null),
            CodeListType.EbixDenmark => (EbixDocumentWriter.EbixCodeList, EbixDocumentWriter.CountryCodeDenmark),
            _ => throw new ArgumentOutOfRangeException(nameof(codeListType), codeListType, "Invalid CodeListType"),
        };
}
