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

using System.Reflection;
using Energinet.DataHub.EDI.B2CWebApi.Mappers;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using FluentAssertions;
using Xunit;
using DocumentType = Energinet.DataHub.EDI.B2CWebApi.Models.DocumentType;

namespace Energinet.DataHub.EDI.Tests.B2CWebApi.Mappers;

public class DocumentTypeMapperTests
{
    public static IEnumerable<object[]> GetAllB2CDocumentTypes()
    {
        var documentsTypes =
            typeof(DocumentType).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

        return documentsTypes.Select(f => new[] { f.GetValue(null)! });
    }

    [Theory]
    [MemberData(nameof(GetAllB2CDocumentTypes))]
    public void Ensure_all_DocumentTypes(DocumentType documentType)
    {
        // Act
        var act = () => DocumentTypeMapper.FromDocumentTypes(new[] { documentType });

        // Assert
        var result = act.Should().NotThrow().Subject;
        result.Should().NotBeNull();
    }

    [Fact]
    public void Ensure_all_DocumentTypes_are_supported()
    {
        // Arrange
        var documentTypes =
            typeof(DocumentType).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Select(f => f.GetValue(null) as DocumentType?)
                .Where(dt => dt != null)
                .Select(dt => dt.ToString())
                .ToList();

        var supportedDocumentTypes =
            typeof(Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.DocumentType).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Select(f => f.GetValue(null) as Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.DocumentType)
                .Where(dt => dt != null)
                .Select(dt => dt!.Name)
                .Concat(
                    typeof(IncomingDocumentType).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                        .Select(f => f.GetValue(null) as IncomingDocumentType)
                        .Where(dt => dt != null)
                        .Select(dt => dt!.Name));

        // TODO - Remove this line when all DocumentTypes are supported in B2C
        supportedDocumentTypes = supportedDocumentTypes
            .Where(x => x != IncomingDocumentType.MeteredDataForMeasurementPoint.Name);

        // Act & Assert
        documentTypes.Should().BeEquivalentTo(supportedDocumentTypes.ToList());
    }
}
