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
using Energinet.DataHub.EDI.IncomingMessages.Domain.Schemas.Cim.Xml;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Schemas.Ebix;
using Energinet.DataHub.EDI.Tests.DocumentValidation;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.Asserts;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.RSM012;

namespace Energinet.DataHub.EDI.OutgoingMessages.IntegrationTests.DocumentAsserters;

public class NotifyValidatedMeasureDataDocumentAsserter
{
    private static readonly DocumentValidator _xmlDocumentValidator = new(
    [
        new CimXmlValidator(new CimXmlSchemaProvider(new CimXmlSchemas())),
        new EbixValidator(new EbixSchemaProvider()),
    ]);

    public static AssertXmlDocument CreateCimXmlAsserter(Stream document) =>
        AssertXmlDocument.Document(
            document,
            "cim_",
            _xmlDocumentValidator);

    public static async Task AssertCorrectDocumentAsync(
        DocumentFormat documentFormat,
        Stream document,
        NotifyValidatedMeasureDataDocumentAssertionInput assertionInput)
    {
        ArgumentNullException.ThrowIfNull(documentFormat);
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(assertionInput);

        IAssertMeteredDateForMeteringPointDocumentDocument asserter = documentFormat.Name switch
        {
            nameof(DocumentFormat.Xml) => new AssertMeteredDateForMeteringPointXmlDocument(
                CreateCimXmlAsserter(document)),
            nameof(DocumentFormat.Json) => new AssertMeteredDateForMeteringPointJsonDocument(document),
            _ => throw new ArgumentOutOfRangeException(nameof(documentFormat), documentFormat, null),
        };

        var requiredHeaderDocumentFields = assertionInput.RequiredHeaderDocumentFields;

        // Required fields
        asserter
            .MessageIdExists()
            .HasBusinessReason(requiredHeaderDocumentFields.BusinessReasonCode)
            .HasSenderId(
                requiredHeaderDocumentFields.SenderId,
                requiredHeaderDocumentFields.SenderScheme)
            .HasSenderRole(requiredHeaderDocumentFields.SenderRole)
            .HasReceiverId(
                requiredHeaderDocumentFields.ReceiverId,
                requiredHeaderDocumentFields.ReceiverScheme)
            .HasReceiverRole(requiredHeaderDocumentFields.ReceiverRole)
            .HasTimestamp(requiredHeaderDocumentFields.Timestamp);

        var optionalHeaderDocumentFields = assertionInput.OptionalHeaderDocumentFields;

        // Optional fields
        asserter.HasBusinessSectorType(optionalHeaderDocumentFields.BusinessSectorType);

        foreach (var assertSeriesDocumentFieldsInput in optionalHeaderDocumentFields.AssertSeriesDocumentFieldsInput)
        {
            var (seriesIndex, requiredSeriesFields, optionalSeriesFields) = assertSeriesDocumentFieldsInput;

            // Required series fields
            asserter
                .HasTransactionId(seriesIndex, requiredSeriesFields.TransactionId)
                .HasMeteringPointNumber(
                    seriesIndex,
                    requiredSeriesFields.MeteringPointNumber,
                    requiredSeriesFields.MeteringPointScheme)
                .HasMeteringPointType(seriesIndex, requiredSeriesFields.MeteringPointType)
                .HasQuantityMeasureUnit(seriesIndex, requiredSeriesFields.QuantityMeasureUnit)
                // Required period fields
                .HasResolution(seriesIndex, requiredSeriesFields.RequiredPeriodDocumentFields.Resolution)
                .HasStartedDateTime(seriesIndex, requiredSeriesFields.RequiredPeriodDocumentFields.StartedDateTime)
                .HasEndedDateTime(seriesIndex, requiredSeriesFields.RequiredPeriodDocumentFields.EndedDateTime)
                .HasPoints(seriesIndex, requiredSeriesFields.RequiredPeriodDocumentFields.Points.ToList().AsReadOnly());

            // Optional series fields
            asserter
                .HasOriginalTransactionIdReferenceId(seriesIndex, optionalSeriesFields.OriginalTransactionIdReferenceId)
                .HasRegistrationDateTime(seriesIndex, optionalSeriesFields.RegistrationDateTime)
                .HasProduct(seriesIndex, optionalSeriesFields.Product)
                .HasInDomain(seriesIndex, optionalSeriesFields.InDomain)
                .HasOutDomain(seriesIndex, optionalSeriesFields.OutDomain);
        }

        await asserter
            .DocumentIsValidAsync();
    }
}
