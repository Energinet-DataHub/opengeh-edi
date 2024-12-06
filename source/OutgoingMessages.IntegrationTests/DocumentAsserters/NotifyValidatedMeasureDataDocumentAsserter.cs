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

        IAssertMeteredDateForMeasurementPointDocumentDocument asserter = documentFormat.Name switch
        {
            nameof(DocumentFormat.Xml) => new AssertMeteredDateForMeasurementPointXmlDocument(
                CreateCimXmlAsserter(document)),
            nameof(DocumentFormat.Json) => new AssertMeteredDateForMeasurementPointJsonDocument(document),
            _ => throw new ArgumentOutOfRangeException(nameof(documentFormat), documentFormat, null),
        };

        asserter
            .MessageIdExists()
            .HasBusinessReason(assertionInput.RequiredDocumentFields.BusinessReasonCode)
            .HasSenderId(
                assertionInput.RequiredDocumentFields.SenderId,
                assertionInput.RequiredDocumentFields.SenderScheme)
            .HasSenderRole(assertionInput.RequiredDocumentFields.SenderRole)
            .HasReceiverId(
                assertionInput.RequiredDocumentFields.ReceiverId,
                assertionInput.RequiredDocumentFields.ReceiverScheme)
            .HasReceiverRole(assertionInput.RequiredDocumentFields.ReceiverRole)
            .HasTimestamp(assertionInput.RequiredDocumentFields.Timestamp);

        if (assertionInput.RequiredSeriesFields != null)
        {
            asserter
                .HasTransactionId(assertionInput.RequiredSeriesFields.TransactionId)
                .HasMeteringPointNumber(
                    assertionInput.RequiredSeriesFields.MeteringPointNumber,
                    assertionInput.RequiredSeriesFields.MeteringPointScheme)
                .HasMeteringPointType(assertionInput.RequiredSeriesFields.MeteringPointType)
                .HasQuantityMeasureUnit(assertionInput.RequiredSeriesFields.QuantityMeasureUnit)
                .HasResolution(assertionInput.RequiredSeriesFields.Resolution)
                .HasStartedDateTime(assertionInput.RequiredSeriesFields.StartedDateTime)
                .HasEndedDateTime(assertionInput.RequiredSeriesFields.EndedDateTime);
            //.HasPoints(assertionInput.RequiredSeriesFields.Points);
        }

        await asserter
            .DocumentIsValidAsync();
    }
}
