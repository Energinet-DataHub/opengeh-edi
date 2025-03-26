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

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.RSM012;

public class AssertMeteredDataForMeteringPointEbixDocument : IAssertMeteredDateForMeteringPointDocumentDocument
{
    private readonly AssertEbixDocument _documentAsserter;

    public AssertMeteredDataForMeteringPointEbixDocument(
        AssertEbixDocument documentAsserter,
        bool skipIdentificationLengthValdation = false)
    {
        _documentAsserter = documentAsserter;
        _documentAsserter.HasValue("type", "E66");
    }

    public IAssertMeteredDateForMeteringPointDocumentDocument HasBusinessReason(string expectedBusinessReasonCode)
    {
        _documentAsserter.HasValueWithAttributes(
            "ProcessEnergyContext/EnergyBusinessProcess",
            expectedBusinessReasonCode,
            CreateRequiredListAttributes(expectedBusinessReasonCode.StartsWith('D') ? CodeListType.EbixDenmark : CodeListType.Ebix));
        return this;
    }

    public IAssertMeteredDateForMeteringPointDocumentDocument HasBusinessSectorType(string? expectedBusinessSectorType)
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

    public IAssertMeteredDateForMeteringPointDocumentDocument HasEndedDateTime(int seriesIndex, string expectedEndedDateTime)
    {
        _documentAsserter.HasValue($"PayloadEnergyTimeSeries[{seriesIndex}]/ObservationTimeSeriesPeriod/End", expectedEndedDateTime);
        return this;
    }

    // Not sure how this looks in ebix?
    public IAssertMeteredDateForMeteringPointDocumentDocument HasInDomain(int seriesIndex, string? expectedInDomain)
    {
        if (expectedInDomain is null)
        {
            _documentAsserter.IsNotPresent($"PayloadEnergyTimeSeries[{seriesIndex}]/InDomain");
            return this;
        }

        _documentAsserter.HasValue($"PayloadEnergyTimeSeries[{seriesIndex}]/InDomain", expectedInDomain);
        return this;
    }

    public IAssertMeteredDateForMeteringPointDocumentDocument HasMeteringPointNumber(int seriesIndex, string expectedMeteringPointNumber, string expectedSchemeCode)
    {
        _documentAsserter.HasValue($"PayloadEnergyTimeSeries[{seriesIndex}]/MeteringPointDomainLocation/Identification", expectedMeteringPointNumber);
        return this;
    }

    public IAssertMeteredDateForMeteringPointDocumentDocument HasMeteringPointType(int seriesIndex, MeteringPointType expectedMeteringPointType)
    {
        _documentAsserter.HasValue($"PayloadEnergyTimeSeries[{seriesIndex}]/DetailMeasurementMeteringPointCharacteristic/TypeOfMeteringPoint", expectedMeteringPointType.Code);
        return this;
    }

    // Not sure how this looks in ebix?
    public IAssertMeteredDateForMeteringPointDocumentDocument HasOriginalTransactionIdReferenceId(int seriesIndex, string? expectedOriginalTransactionIdReferenceId)
    {
        if (expectedOriginalTransactionIdReferenceId is null)
        {
            _documentAsserter.IsNotPresent($"PayloadEnergyTimeSeries[{seriesIndex}]/OriginalTransactionId");
            return this;
        }

        _documentAsserter.HasValue($"PayloadEnergyTimeSeries[{seriesIndex}]/OriginalTransactionId", expectedOriginalTransactionIdReferenceId);
        return this;
    }

    // Not sure how this looks in ebix?
    public IAssertMeteredDateForMeteringPointDocumentDocument HasOutDomain(int seriesIndex, string? expectedOutDomain)
    {
        if (expectedOutDomain is null)
        {
            _documentAsserter.IsNotPresent($"PayloadEnergyTimeSeries[{seriesIndex}]/OutDomain");
            return this;
        }

        _documentAsserter.HasValue($"PayloadEnergyTimeSeries[{seriesIndex}]/OutDomain", expectedOutDomain);
        return this;
    }

    public IAssertMeteredDateForMeteringPointDocumentDocument HasPoints(int seriesIndex, IReadOnlyList<AssertPointDocumentFieldsInput> expectedPoints)
    {
        var pointsInDocument = _documentAsserter
            .GetElements($"PayloadEnergyTimeSeries[{seriesIndex}]/IntervalEnergyObservation");

        pointsInDocument.Should().HaveSameCount(expectedPoints);

        for (var i = 0; i < expectedPoints.Count; i++)
        {
            var (requiredPointDocumentFields, optionalPointDocumentFields) = expectedPoints[i];

            _documentAsserter
                .HasValue(
                    $"PayloadEnergyTimeSeries[{seriesIndex}]/IntervalEnergyObservation[{i + 1}]/position",
                    requiredPointDocumentFields.Position.ToString());

            if (optionalPointDocumentFields.Quantity.HasValue)
            {
                _documentAsserter
                    .HasValue(
                        $"PayloadEnergyTimeSeries[{seriesIndex}]/IntervalEnergyObservation[{i + 1}]/quantity",
                        optionalPointDocumentFields.Quantity.Value.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                AssertElementNotPresent($"PayloadEnergyTimeSeries[{seriesIndex}]/IntervalEnergyObservation[{i + 1}]/quantity");
            }

            if (optionalPointDocumentFields.Quality != null)
            {
                _documentAsserter
                    .HasValue(
                        $"PayloadEnergyTimeSeries[{seriesIndex}]/IntervalEnergyObservation[{i + 1}]/quality",
                        optionalPointDocumentFields.Quality.Code);
            }
            else
            {
                AssertElementNotPresent($"PayloadEnergyTimeSeries[{seriesIndex}]/IntervalEnergyObservation[{i + 1}]/quality");
            }
        }

        return this;

        void AssertElementNotPresent(string xpath)
        {
            _documentAsserter.IsNotPresent(xpath);
        }
    }

    // Not sure how this looks in ebix?
    public IAssertMeteredDateForMeteringPointDocumentDocument HasProduct(int seriesIndex, string? expectedProduct)
    {
        if (expectedProduct is null)
        {
            _documentAsserter.IsNotPresent($"Series[{seriesIndex}]/Product");
            return this;
        }

        _documentAsserter.HasValue($"Series[{seriesIndex}]/Product", expectedProduct);
        return this;
    }

    // Not sure how this looks in ebix?
    public IAssertMeteredDateForMeteringPointDocumentDocument HasQuantityMeasureUnit(int seriesIndex, string expectedQuantityMeasureUnit)
    {
        _documentAsserter.IsNotPresent($"PayloadEnergyTimeSeries[{seriesIndex}]/QuantityMeasureUnit");
        return this;
    }

    // Not sure how this looks in ebix?
    public IAssertMeteredDateForMeteringPointDocumentDocument HasReceiverId(string expectedReceiverId, string expectedSchemeCode)
    {
        _documentAsserter.IsNotPresent($"Receiver");
        return this;
    }

    // Not sure how this looks in ebix?
    public IAssertMeteredDateForMeteringPointDocumentDocument HasReceiverRole(string expectedReceiverRole)
    {
        _documentAsserter.IsNotPresent($"ReceiverRole");
        return this;
    }

    // Not sure how this looks in ebix?
    public IAssertMeteredDateForMeteringPointDocumentDocument HasRegistrationDateTime(int seriesIndex, string? expectedRegistrationDateTime)
    {
        if (expectedRegistrationDateTime is null)
        {
            _documentAsserter.IsNotPresent($"Series[{seriesIndex}]/RegistrationDateTime");
            return this;
        }

        _documentAsserter.HasValue($"Series[{seriesIndex}]/RegistrationDateTime", expectedRegistrationDateTime);
        return this;
    }

    public IAssertMeteredDateForMeteringPointDocumentDocument HasResolution(int seriesIndex, string expectedResolution)
    {
        _documentAsserter.HasValue($"PayloadEnergyTimeSeries[{seriesIndex}]/ObservationTimeSeriesPeriod/ResolutionDuration", expectedResolution);
        return this;
    }

    // Not sure how this looks in ebix?
    public IAssertMeteredDateForMeteringPointDocumentDocument HasSenderId(string expectedSenderId, string expectedSchemeCode)
    {
        _documentAsserter.IsNotPresent($"SenderId");
        return this;
    }

    // Not sure how this looks in ebix?
    public IAssertMeteredDateForMeteringPointDocumentDocument HasSenderRole(string expectedSenderRole)
    {
        _documentAsserter.IsNotPresent($"SenderRole");
        return this;
    }

    public IAssertMeteredDateForMeteringPointDocumentDocument HasStartedDateTime(int seriesIndex, string expectedStartedDateTime)
    {
        _documentAsserter.HasValue($"PayloadEnergyTimeSeries[{seriesIndex}]/ObservationTimeSeriesPeriod/Start", expectedStartedDateTime);
        return this;
    }

    public IAssertMeteredDateForMeteringPointDocumentDocument HasTimestamp(string expectedTimestamp)
    {
        _documentAsserter.HasValue($"HeaderEnergyDocument/Creation", expectedTimestamp);
        return this;
    }

    public IAssertMeteredDateForMeteringPointDocumentDocument HasTransactionId(int seriesIndex, TransactionId expectedTransactionId)
    {
        _documentAsserter.HasValue($"PayloadEnergyTimeSeries[{seriesIndex}]/Identification", expectedTransactionId.Value);
        return this;
    }

    public IAssertMeteredDateForMeteringPointDocumentDocument MessageIdExists()
    {
        _documentAsserter.ElementExists("MessageReference");
        return this;
    }

    public IAssertMeteredDateForMeteringPointDocumentDocument TransactionIdExists(int seriesIndex)
    {
        _documentAsserter.ElementExists($"PayloadEnergyTimeSeries[{seriesIndex}]/Identification");
        return this;
    }

    public async Task<IAssertMeteredDateForMeteringPointDocumentDocument> DocumentIsValidAsync()
    {
        await _documentAsserter.HasValidStructureAsync(DocumentType.NotifyValidatedMeasureData).ConfigureAwait(false);
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
