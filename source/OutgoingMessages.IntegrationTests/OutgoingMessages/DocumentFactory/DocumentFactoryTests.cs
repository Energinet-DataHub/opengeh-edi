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
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters;
using Energinet.DataHub.EDI.OutgoingMessages.IntegrationTests.Fixtures;
using FluentAssertions;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.OutgoingMessages.IntegrationTests.OutgoingMessages.DocumentFactory;

public class DocumentFactoryTests
    : OutgoingMessagesTestBase
{
    private readonly IEnumerable<IDocumentWriter> _documentWriters;

    public DocumentFactoryTests(OutgoingMessagesTestFixture outgoingMessagesTestFixture, ITestOutputHelper testOutputHelper)
        : base(outgoingMessagesTestFixture, testOutputHelper)
    {
        _documentWriters = GetService<IEnumerable<IDocumentWriter>>();
    }

    public static IEnumerable<object[]> GetDocumentTypes()
    {
        var documentTypes = EnumerationType.GetAll<DocumentType>();
        return documentTypes.Select(document => new object[] { document }).ToList();
    }

    [Theory]
    [MemberData(nameof(GetDocumentTypes))]
    public void Ensure_that_all_document_support_xml(DocumentType documentType)
    {
        var writer = _documentWriters.FirstOrDefault(writer =>
            writer.HandlesType(documentType) && writer.HandlesFormat(DocumentFormat.Xml));

        writer.Should().NotBeNull();
    }

    [Theory]
    [MemberData(nameof(GetDocumentTypes))]
    public void Ensure_that_all_document_support_json(DocumentType documentType)
    {
        var writer = _documentWriters.FirstOrDefault(writer =>
            writer.HandlesType(documentType) && writer.HandlesFormat(DocumentFormat.Json));

        writer.Should().NotBeNull();
    }

    [Theory]
    [MemberData(nameof(GetDocumentTypes))]
    public void Ensure_that_all_document_support_ebix(DocumentType documentType)
    {
        var writer = _documentWriters.FirstOrDefault(writer =>
            writer.HandlesType(documentType) && writer.HandlesFormat(DocumentFormat.Ebix));

        if (documentType == DocumentType.NotifyValidatedMeasureData)
        {
            writer.Should().BeNull($"{documentType} does not support ebix");
        }
        else
        {
            writer.Should().NotBeNull();
        }
    }
}
