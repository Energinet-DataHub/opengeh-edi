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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats.Ebix;
using Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.Asserts;
using Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.NotifyWholesaleServices;
using NodaTime;

namespace Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.RSM018;

public class AssertMissingMeasurementEbixDocument : IAssertMissingMeasurementDocument
{
    private readonly AssertEbixDocument _documentAsserter;

    public AssertMissingMeasurementEbixDocument(AssertEbixDocument documentAsserter)
    {
        _documentAsserter = documentAsserter;
        _documentAsserter.HasValue("HeaderEnergyDocument/DocumentType", "D24");
        _documentAsserter.HasValueWithAttributes(
            "ProcessEnergyContext/EnergyIndustryClassification",
            "23",
            CreateRequiredListAttributes(CodeListType.UnitedNations));
    }

    public async Task<IAssertMissingMeasurementDocument> DocumentIsValidAsync()
    {
        await _documentAsserter.HasValidStructureAsync(DocumentType.ReminderOfMissingMeasureData, "3").ConfigureAwait(false);
        return this;
    }

    public Task<IAssertMissingMeasurementDocument> HasBusinessReason(BusinessReason businessReason)
    {
        throw new NotImplementedException();
    }

    public Task<IAssertMissingMeasurementDocument> HasSenderId(ActorNumber actorNumber)
    {
        throw new NotImplementedException();
    }

    public Task<IAssertMissingMeasurementDocument> HasSenderRole(ActorRole actorRole)
    {
        throw new NotImplementedException();
    }

    public Task<IAssertMissingMeasurementDocument> HasReceiverId(ActorNumber actorNumber)
    {
        throw new NotImplementedException();
    }

    public Task<IAssertMissingMeasurementDocument> HasReceiverRole(ActorRole actorRole)
    {
        throw new NotImplementedException();
    }

    public Task<IAssertMissingMeasurementDocument> HasTimestamp(Instant timestamp)
    {
        throw new NotImplementedException();
    }

    public Task<IAssertMissingMeasurementDocument> HasTransactionId(int seriesIndex, TransactionId expectedTransactionId)
    {
        throw new NotImplementedException();
    }

    public Task<IAssertMissingMeasurementDocument> HasMeteringPointNumber(int seriesIndex, string meteringPointNumber)
    {
        throw new NotImplementedException();
    }

    public Task<IAssertMissingMeasurementDocument> HasMissingDate(int seriesIndex, Instant missingDate)
    {
        throw new NotImplementedException();
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
