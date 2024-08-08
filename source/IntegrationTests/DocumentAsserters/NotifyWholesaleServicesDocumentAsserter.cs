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
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Schemas.Cim.Xml;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Schemas.Ebix;
using Energinet.DataHub.Edi.Responses;
using Energinet.DataHub.EDI.Tests.DocumentValidation;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.Asserts;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.NotifyWholesaleServices;

namespace Energinet.DataHub.EDI.IntegrationTests.DocumentAsserters;

public static class NotifyWholesaleServicesDocumentAsserter
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

    public static async Task AssertCorrectDocumentAsync(DocumentFormat documentFormat, Stream document, NotifyWholesaleServicesDocumentAssertionInput assertionInput)
    {
        ArgumentNullException.ThrowIfNull(documentFormat);
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(assertionInput);

        IAssertNotifyWholesaleServicesDocument asserter = documentFormat.Name switch
        {
            nameof(DocumentFormat.Xml) => new AssertNotifyWholesaleServicesXmlDocument(CreateCimXmlAsserter(document)),
            nameof(DocumentFormat.Json) => new AssertNotifyWholesaleServicesJsonDocument(document),
            nameof(DocumentFormat.Ebix) => new AssertNotifyWholesaleServicesEbixDocument(CreateEbixAsserter(document), true),
            _ => throw new ArgumentOutOfRangeException(nameof(documentFormat), documentFormat, null),
        };

        asserter
            // -- Assert header values --
            .MessageIdExists()
            // Assert businessSector.type? (23)
            .HasBusinessReason(assertionInput.BusinessReasonWithSettlementVersion.BusinessReason, CodeListType.EbixDenmark)
            .HasTimestamp(assertionInput.Timestamp)
            .HasReceiverId(ActorNumber.Create(assertionInput.ReceiverId))
            .HasReceiverRole(assertionInput.ReceiverRole, CodeListType.Ebix)
            .HasSenderId(ActorNumber.Create(assertionInput.SenderId), "A10")
            .HasSenderRole(assertionInput.SenderRole)
            // Assert type? (E31)
            // -- Assert series values --
            .TransactionIdExists()
            .HasCurrency(assertionInput.Currency)
            .HasEnergySupplierNumber(ActorNumber.Create(assertionInput.EnergySupplierNumber), "A10")
            .HasGridAreaCode(assertionInput.GridArea, "NDK")
            .HasProductCode(assertionInput.ProductCode)
            .HasQuantityMeasurementUnit(assertionInput.QuantityMeasurementUnit)
            .HasCalculationVersion(assertionInput.CalculationVersion)
            .HasResolution(assertionInput.Resolution)
            .HasPeriod(assertionInput.Period);

        var isTotalMonthlyAmount = assertionInput.Points.First().Price is null;
        if (!isTotalMonthlyAmount)
        {
            asserter.HasPoints(assertionInput.Points, assertionInput.Resolution);
        }
        else
        {
            // QuantityQuality should not be present in any format if resolution is monthly
            asserter.HasSinglePointWithAmountAndQuality(assertionInput.Points.First().Amount, null);
        }

        if (assertionInput.PriceMeasurementUnit is not null)
            asserter.HasPriceMeasurementUnit(assertionInput.PriceMeasurementUnit);
        else
            asserter.PriceMeasurementUnitDoesNotExist();

        if (assertionInput.MeteringPointType is not null)
            asserter.HasMeteringPointType(assertionInput.MeteringPointType);
        else
            asserter.MeteringPointTypeDoesNotExist();

        if (assertionInput.SettlementMethod is not null)
            asserter.HasSettlementMethod(assertionInput.SettlementMethod);
        else
            asserter.SettlementMethodDoesNotExist();

        if (assertionInput.ChargeType is not null)
            asserter.HasChargeType(assertionInput.ChargeType);
        else
            asserter.ChargeTypeDoesNotExist();

        if (assertionInput.ChargeCode is not null)
            asserter.HasChargeCode(assertionInput.ChargeCode);
        else
            asserter.ChargeCodeDoesNotExist();

        if (assertionInput.ChargeTypeOwner is not null)
            asserter.HasChargeTypeOwner(ActorNumber.Create(assertionInput.ChargeTypeOwner), "A10");
        else
            asserter.ChargeTypeOwnerDoesNotExist();

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
