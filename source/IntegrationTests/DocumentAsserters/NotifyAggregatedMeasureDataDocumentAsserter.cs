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
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Schemas.Cim.Xml;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Schemas.Ebix;
using Energinet.DataHub.EDI.Tests.DocumentValidation;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.Asserts;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.NotifyAggregatedMeasureData;

namespace Energinet.DataHub.EDI.IntegrationTests.DocumentAsserters;

public static class NotifyAggregatedMeasureDataDocumentAsserter
{
    private static readonly DocumentValidator _xmlDocumentValidator = new(new List<IValidator>
    {
        new CimXmlValidator(new CimXmlSchemaProvider(new CimXmlSchemas())),
        new EbixValidator(new EbixSchemaProvider()),
    });

    public static AssertEbixDocument CreateEbixAsserter(Stream document)
    {
        return AssertEbixDocument.Document(
            document,
            "ns0",
            _xmlDocumentValidator);
    }

    public static AssertXmlDocument CreateCimXmlAsserter(Stream document)
    {
        return AssertXmlDocument.Document(
            document,
            "cim_",
            _xmlDocumentValidator);
    }

    public static async Task AssertCorrectDocumentAsync(DocumentFormat documentFormat, Stream document, NotifyAggregatedMeasureDataDocumentAssertionInput assertionInput)
    {
        ArgumentNullException.ThrowIfNull(documentFormat);
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(assertionInput);

        IAssertNotifyAggregatedMeasureDataDocument asserter = documentFormat.Name switch
        {
            nameof(DocumentFormat.Xml) => new AssertNotifyAggregatedMeasureDataXmlDocument(CreateCimXmlAsserter(document)),
            nameof(DocumentFormat.Json) => new AssertNotifyAggregatedMeasureDataJsonDocument(document),
            nameof(DocumentFormat.Ebix) => new AssertNotifyAggregatedMeasureDataEbixDocument(CreateEbixAsserter(document), true),
            _ => throw new ArgumentOutOfRangeException(nameof(documentFormat), documentFormat, null),
        };

        asserter
            // -- Assert header values --
            .MessageIdExists()
            // Assert type? (E31)
            .HasBusinessReason(assertionInput.BusinessReasonWithSettlementVersion.BusinessReason)
            // Assert businessSector.type? (23)
            .HasSenderId(assertionInput.SenderId.Value)
            // .HasSenderRole(assertionInput.SenderRole) ?
            .HasReceiverId(assertionInput.ReceiverId.Value)
            // .HasReceiverRole(assertionInput.ReceiverRole)
            .HasTimestamp(assertionInput.Timestamp)
            // -- Assert series values --
            .TransactionIdExists()
            .HasCalculationResultVersion(assertionInput.CalculationVersion)
            .HasMeteringPointType(assertionInput.MeteringPointType)
            .HasGridAreaCode(assertionInput.GridAreaCode)
            .HasProductCode(assertionInput.ProductCode)
            .HasQuantityMeasurementUnit(assertionInput.QuantityMeasurementUnit)
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
            asserter.SettlementVersionIsNotPresent();
        }

        if (assertionInput.SettlementMethod is not null)
            asserter.HasSettlementMethod(assertionInput.SettlementMethod);
        else
            asserter.SettlementMethodIsNotPresent();

        if (assertionInput.EnergySupplierNumber != null)
            asserter.HasEnergySupplierNumber(assertionInput.EnergySupplierNumber.Value);
        else
            asserter.EnergySupplierNumberIsNotPresent();

        if (assertionInput.BalanceResponsibleNumber != null)
            asserter.HasBalanceResponsibleNumber(assertionInput.BalanceResponsibleNumber.Value);
        else
            asserter.BalanceResponsibleNumberIsNotPresent();

        await asserter
            .DocumentIsValidAsync();
    }
}
