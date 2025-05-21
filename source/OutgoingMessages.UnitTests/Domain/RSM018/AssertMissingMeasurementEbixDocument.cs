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
using FluentAssertions;
using NodaTime;

namespace Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.RSM018;

public class AssertMissingMeasurementEbixDocument : IAssertMissingMeasurementDocument
{
    private const string HeaderEnergyDocument = "HeaderEnergyDocument";
    private const string ProcessEnergyContext = "ProcessEnergyContext";
    private const string PayloadMissingDataRequest = "PayloadMissingDataRequest";
    private const string Gs1Code = "9";

    private readonly AssertEbixDocument _documentAsserter;

    public AssertMissingMeasurementEbixDocument(AssertEbixDocument documentAsserter)
    {
        _documentAsserter = documentAsserter;
        _documentAsserter.HasValue($"{HeaderEnergyDocument}/DocumentType", "D24");
        _documentAsserter.HasValueWithAttributes(
            "ProcessEnergyContext/EnergyIndustryClassification",
            "23",
            CreateRequiredListAttributes(CodeListType.UnitedNations));
        // Number of reminders are hardcoded to 0.
        _documentAsserter.HasValue(
            $"{PayloadMissingDataRequest}[{1}]/NumberOfReminders",
            "0");
    }

    public async Task<IAssertMissingMeasurementDocument> DocumentIsValidAsync()
    {
        await _documentAsserter.HasValidStructureAsync(DocumentType.ReminderOfMissingMeasureData, "3").ConfigureAwait(false);
        return this;
    }

    public IAssertMissingMeasurementDocument HasMessageId(MessageId messageId)
    {
        _documentAsserter.HasValue($"{HeaderEnergyDocument}/Identification", messageId.Value);
        return this;
    }

    public IAssertMissingMeasurementDocument HasBusinessReason(BusinessReason businessReason)
    {
        _documentAsserter.HasValueWithAttributes(
            $"{ProcessEnergyContext}/EnergyBusinessProcess",
            EbixCode.Of(businessReason),
            CreateRequiredListAttributes(CodeListType.EbixDenmark));

        return this;
    }

    public IAssertMissingMeasurementDocument HasSenderId(ActorNumber actorNumber)
    {
        _documentAsserter.HasValue($"{HeaderEnergyDocument}/SenderEnergyParty/Identification", actorNumber.Value);
        return this;
    }

    public IAssertMissingMeasurementDocument HasSenderRole(ActorRole actorRole)
    {
        // Ebix doesn't have a sender role
        return this;
    }

    public IAssertMissingMeasurementDocument HasReceiverId(ActorNumber actorNumber)
    {
        _documentAsserter.HasValue($"{HeaderEnergyDocument}/RecipientEnergyParty/Identification", actorNumber.Value);
        return this;
    }

    public IAssertMissingMeasurementDocument HasReceiverRole(ActorRole actorRole)
    {
        _documentAsserter.HasValueWithAttributes(
            "ProcessEnergyContext/EnergyBusinessProcessRole",
            EbixCode.Of(actorRole),
            CreateRequiredListAttributes(CodeListType.Ebix));
        return this;
    }

    public IAssertMissingMeasurementDocument HasTimestamp(Instant timestamp)
    {
        _documentAsserter.HasValue($"{HeaderEnergyDocument}/Creation", timestamp.ToString());
        return this;
    }

    public IAssertMissingMeasurementDocument HasTransactionId(int seriesIndex, TransactionId expectedTransactionId)
    {
        _documentAsserter.HasValue($"{PayloadMissingDataRequest}[{seriesIndex}]/Identification", expectedTransactionId.Value);
        return this;
    }

    public IAssertMissingMeasurementDocument HasMeteringPointNumber(int seriesIndex, MeteringPointId meteringPointNumber)
    {
        _documentAsserter.HasValueWithAttributes(
            $"{PayloadMissingDataRequest}[{seriesIndex}]/MeteringPointDomainLocation/Identification",
            meteringPointNumber.Value,
            new AttributeNameAndValue("schemeAgencyIdentifier", Gs1Code));
        return this;
    }

    public IAssertMissingMeasurementDocument HasMissingDate(int seriesIndex, Instant missingDate)
    {
        _documentAsserter.HasValue($"{PayloadMissingDataRequest}[{seriesIndex}]/RequestPeriod", missingDate.ToString());
        return this;
    }

    public IAssertMissingMeasurementDocument HasMissingData(
        IReadOnlyCollection<(MeteringPointId MeteringPointId, Instant Date)> missingData)
    {
        for (int i = 0; i < missingData.Count; i++)
        {
            missingData.Should()
                .ContainSingle(
                    data =>
                        data.MeteringPointId.Value == _documentAsserter.GetElement($"{PayloadMissingDataRequest}[{i + 1}]/MeteringPointDomainLocation/Identification")!.Value
                        && data.Date.ToString() == _documentAsserter.GetElement($"{PayloadMissingDataRequest}[{i + 1}]/RequestPeriod")!.Value);
        }

        _documentAsserter.GetElements($"{PayloadMissingDataRequest}").Should().HaveCount(missingData.Count);
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
