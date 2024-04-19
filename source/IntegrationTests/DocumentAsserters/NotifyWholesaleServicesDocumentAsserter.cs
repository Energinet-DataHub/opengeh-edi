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
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.DocumentValidation.CimXml;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.DocumentValidation.Ebix;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.Asserts;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.NotifyWholesaleServices;

namespace Energinet.DataHub.EDI.IntegrationTests.DocumentAsserters;

public static class NotifyWholesaleServicesDocumentAsserter
{
    public static async Task AssertCorrectDocumentAsync(DocumentFormat documentFormat, Stream document, NotifyWholesaleServicesDocumentAssertionInput assertionInput)
    {
        ArgumentNullException.ThrowIfNull(documentFormat);
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(assertionInput);

        var xmlDocumentValidator = new DocumentValidator(new List<IValidator>
        {
            new CimXmlValidator(new CimXmlSchemaProvider()),
            new EbixValidator(new EbixSchemaProvider()),
        });
        IAssertNotifyWholesaleServicesDocument asserter = documentFormat.Name switch
        {
            nameof(DocumentFormat.Xml) => new AssertNotifyWholesaleServicesXmlDocument(
                AssertXmlDocument.Document(
                    document,
                    "cim_",
                    xmlDocumentValidator)),
            nameof(DocumentFormat.Json) => new AssertNotifyWholesaleServicesJsonDocument(document),
            nameof(DocumentFormat.Ebix) => new AssertNotifyWholesaleServicesEbixDocument(
                AssertEbixDocument.Document(
                    document,
                    "ns0",
                    xmlDocumentValidator),
                true),
            _ => throw new ArgumentOutOfRangeException(nameof(documentFormat), documentFormat, null),
        };

        asserter
            // -- Assert header values --
            .MessageIdExists()
            // Assert businessSector.type? (23)
            .HasTimestamp(assertionInput.Timestamp)
            .HasReceiverId(ActorNumber.Create(assertionInput.ReceiverId))
            .HasReceiverRole(assertionInput.ReceiverRole, CodeListType.Ebix)
            .HasSenderId(ActorNumber.Create(assertionInput.SenderId), "A10")
            .HasSenderRole(assertionInput.SenderRole)
            // Assert type? (E31)
            // -- Assert series values --
            .TransactionIdExists()
            .HasChargeTypeOwner(ActorNumber.Create(assertionInput.ChargeTypeOwner), "A10")
            .HasChargeCode(assertionInput.ChargeCode)
            .HasChargeType(assertionInput.ChargeType)
            .HasCurrency(assertionInput.Currency)
            .HasEnergySupplierNumber(ActorNumber.Create(assertionInput.EnergySupplierNumber), "A10")
            .HasSettlementMethod(assertionInput.SettlementMethod)
            .HasMeteringPointType(assertionInput.MeteringPointType)
            .HasGridAreaCode(assertionInput.GridArea, "NDK")
            .HasPriceMeasurementUnit(assertionInput.PriceMeasurementUnit)
            .HasProductCode(assertionInput.ProductCode)
            .HasQuantityMeasurementUnit(assertionInput.QuantityMeasurementUnit)
            .HasCalculationVersion(assertionInput.CalculationVersion)
            .HasResolution(assertionInput.Resolution)
            .HasPeriod(assertionInput.Period)
            .HasPoints(assertionInput.Points);

        if (assertionInput.OriginalTransactionIdReference != null)
            asserter.HasOriginalTransactionIdReference(assertionInput.OriginalTransactionIdReference);
        else
            asserter.OriginalTransactionIdReferenceDoesNotExist();

        if (assertionInput.BusinessReasonWithSettlementVersion.BusinessReason == BusinessReason.Correction)
        {
            if (assertionInput.BusinessReasonWithSettlementVersion.SettlementVersion == null)
                throw new InvalidOperationException("Settlement version is required when business reason is correction");
            asserter.HasSettlementVersion(assertionInput.BusinessReasonWithSettlementVersion.SettlementVersion);
        }
        else
        {
            asserter.SettlementVersionDoesNotExist();
        }

        await asserter
            .DocumentIsValidAsync();
    }
}
