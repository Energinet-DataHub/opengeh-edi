using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats.Ebix;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.Asserts;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.NotifyWholesaleServices;

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

    public IAssertMeteredDateForMeteringPointDocumentDocument HasInDomain(int seriesIndex, string? expectedInDomain)
    {
        throw new NotImplementedException();
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

    public IAssertMeteredDateForMeteringPointDocumentDocument HasOriginalTransactionIdReferenceId(int seriesIndex, string? expectedOriginalTransactionIdReferenceId)
    {
        throw new NotImplementedException();
    }

    public IAssertMeteredDateForMeteringPointDocumentDocument HasOutDomain(int seriesIndex, string? expectedOutDomain)
    {
        throw new NotImplementedException();
    }

    public IAssertMeteredDateForMeteringPointDocumentDocument HasPoints(int seriesIndex, IReadOnlyList<AssertPointDocumentFieldsInput> expectedPoints)
    {
        throw new NotImplementedException();
    }

    public IAssertMeteredDateForMeteringPointDocumentDocument HasProduct(int seriesIndex, string? expectedProduct)
    {
        throw new NotImplementedException();
    }

    public IAssertMeteredDateForMeteringPointDocumentDocument HasQuantityMeasureUnit(int seriesIndex, string expectedQuantityMeasureUnit)
    {
        throw new NotImplementedException();
    }

    public IAssertMeteredDateForMeteringPointDocumentDocument HasReceiverId(string expectedReceiverId, string expectedSchemeCode)
    {
        throw new NotImplementedException();
    }

    public IAssertMeteredDateForMeteringPointDocumentDocument HasReceiverRole(string expectedReceiverRole)
    {
        throw new NotImplementedException();
    }

    public IAssertMeteredDateForMeteringPointDocumentDocument HasRegistrationDateTime(int seriesIndex, string? expectedRegistrationDateTime)
    {
        throw new NotImplementedException();
    }

    public IAssertMeteredDateForMeteringPointDocumentDocument HasResolution(int seriesIndex, string expectedResolution)
    {
        _documentAsserter.HasValue($"PayloadEnergyTimeSeries[{seriesIndex}]/ObservationTimeSeriesPeriod/ResolutionDuration", expectedResolution);
        return this;
    }

    public IAssertMeteredDateForMeteringPointDocumentDocument HasSenderId(string expectedSenderId, string expectedSchemeCode)
    {
        throw new NotImplementedException();
    }

    public IAssertMeteredDateForMeteringPointDocumentDocument HasSenderRole(string expectedSenderRole)
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }

    public IAssertMeteredDateForMeteringPointDocumentDocument TransactionIdExists(int seriesIndex)
    {
        throw new NotImplementedException();
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
}
