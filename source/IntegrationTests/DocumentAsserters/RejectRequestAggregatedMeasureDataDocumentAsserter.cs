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
using System.IO;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.DocumentValidation;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.DocumentValidation.Cim.CimXml;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.DocumentValidation.Ebix;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.Asserts;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.RejectRequestAggregatedMeasureData;

namespace Energinet.DataHub.EDI.IntegrationTests.DocumentAsserters;

public static class RejectRequestAggregatedMeasureDataDocumentAsserter
{
    public static async Task AssertCorrectDocumentAsync(
        DocumentFormat documentFormat,
        Stream document,
        RejectRequestAggregatedMeasureDataDocumentAssertionInput assertionInput)
    {
        ArgumentNullException.ThrowIfNull(documentFormat);
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(assertionInput);

        var xmlDocumentValidator = new DocumentValidator(new List<IValidator>
        {
            new CimXmlValidator(new CimXmlSchemaProvider(new CimXmlSchemas())),
            new EbixValidator(new EbixSchemaProvider()),
        });
        IAssertRejectRequestAggregatedMeasureDataDocument asserter = documentFormat.Name switch
        {
            nameof(DocumentFormat.Xml) => new AssertRejectRequestAggregatedMeasureDataXmlDocument(
                AssertXmlDocument.Document(
                    document,
                    "cim_",
                    xmlDocumentValidator)),
            nameof(DocumentFormat.Json) => new AssertRejectRequestAggregatedMeasureDataJsonDocument(document),
            nameof(DocumentFormat.Ebix) => new AssertRejectRequestAggregatedMeasureDataEbixDocument(
                AssertEbixDocument.Document(
                    document,
                    "ns0",
                    xmlDocumentValidator),
                true),
            _ => throw new ArgumentOutOfRangeException(nameof(documentFormat), documentFormat, null),
        };

        asserter
            .MessageIdExists()
            // Assert type?
            .HasBusinessReason(assertionInput.BusinessReason)
            .HasSenderId(assertionInput.SenderId)
            //.HasSenderRole(assertionInput.SenderRole)
            .HasReceiverId(assertionInput.ReceiverId)
            //.HasReceiverRole(assertionInput.ReceiverRole)
            .HasTimestamp(assertionInput.Timestamp)
            .HasReasonCode(assertionInput.ReasonCode)
            .TransactionIdExists()
            .HasOriginalTransactionId(assertionInput.OriginalTransactionIdReference)
            .HasSerieReasonCode(assertionInput.SeriesReasonCode)
            .HasSerieReasonMessage(assertionInput.SeriesReasonMessage);

        await asserter.DocumentIsValidAsync();
    }
}
