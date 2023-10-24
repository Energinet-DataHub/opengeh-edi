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

using System.Collections.Generic;
using System.Linq;
using Energinet.DataHub.EDI.Common;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.Process.Domain.Documents;
using Xunit;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.OutgoingMessages.DocumentFactory;

public class DocumentFactoryTests
    : TestBase
{
    private readonly IEnumerable<IDocumentWriter> _documentWriters;

    public DocumentFactoryTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
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
    public void Ensure_that_all_document_writers_support_xml(DocumentType documentType)
    {
        var writer = _documentWriters.FirstOrDefault(writer =>
            writer.HandlesType(documentType) && writer.HandlesFormat(DocumentFormat.Xml));

        Assert.NotNull(writer);
    }

    [Theory]
    [MemberData(nameof(GetDocumentTypes))]
    public void Ensure_that_all_document_writers_but_listed_support_json(DocumentType documentType)
    {
        var writer = _documentWriters.FirstOrDefault(writer =>
            writer.HandlesType(documentType) && writer.HandlesFormat(DocumentFormat.Json));

        Assert.NotNull(writer);
    }

    [Theory]
    [MemberData(nameof(GetDocumentTypes))]
    public void Ensure_that_all_document_writers_but_listed_support_ebix(DocumentType documentType)
    {
        var writer = _documentWriters.FirstOrDefault(writer =>
            writer.HandlesType(documentType) && writer.HandlesFormat(DocumentFormat.Ebix));

        Assert.NotNull(writer);
    }
}
