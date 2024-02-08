using System;
using System.IO;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.Tests.Fixtures;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.AggregationResult;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.Asserts;
using Xunit;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.WholesaleCalculations;

public class WholesaleCalculationResultDocumentWriterTests : IClassFixture<DocumentValidationFixture>
{
    private readonly DocumentValidationFixture _documentValidation;

    public WholesaleCalculationResultDocumentWriterTests(
        DocumentValidationFixture documentValidation)
    {
        _documentValidation = documentValidation;
    }

    [Theory]
    [InlineData(nameof(DocumentFormat.Xml))]
    public async Task Can_ceate_document(string documentFormat)
    {
        var document = null!;

        // Assert
        await AssertDocument(document, DocumentFormat.From(documentFormat))
    }

    private IAssertWholesaleCalculationResultDocument AssertDocument(Stream document, DocumentFormat documentFormat)
    {
         if (documentFormat == DocumentFormat.Xml)
        {
            var assertXmlDocument = AssertXmlDocument.Document(document, "cim", _documentValidation.Validator);
            return new AssertAggregationResultXmlDocument(assertXmlDocument);
        }
        throw new NotSupportedException($"Document format '{documentFormat}' is not supported");
    }
}
