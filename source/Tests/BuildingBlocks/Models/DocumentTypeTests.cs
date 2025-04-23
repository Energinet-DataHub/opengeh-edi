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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace Energinet.DataHub.EDI.Tests.BuildingBlocks.Models;

public class DocumentTypeTests
{
    public static IEnumerable<DocumentType> GetAllDocumentType()
    {
        var fields = typeof(DocumentType).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

        return fields.Select(f => f.GetValue(null)).Cast<DocumentType>();
    }

    [Fact]
    public void Ensure_all_DocumentTypes()
    {
        var documentTypes = new List<(DocumentType ExpectedValue, string Name, byte DatabaseValue)>()
        {
            (DocumentType.RequestAggregatedMeasureData, "Aggregations", 1),
            (DocumentType.NotifyAggregatedMeasureData, "NotifyAggregatedMeasureData", 2),
            (DocumentType.RejectRequestAggregatedMeasureData, "RejectRequestAggregatedMeasureData", 3),
            (DocumentType.RequestWholesaleSettlement, "RequestWholesaleSettlement", 4),
            (DocumentType.NotifyWholesaleServices, "NotifyWholesaleServices", 5),
            (DocumentType.RejectRequestWholesaleSettlement, "RejectRequestWholesaleSettlement", 6),
            (DocumentType.NotifyValidatedMeasureData, "NotifyValidatedMeasureData", 7),
            (DocumentType.Acknowledgement, "Acknowledgement", 8),
        };

        using var scope = new AssertionScope();
        foreach (var test in documentTypes)
        {
            EnumerationType.FromName<DocumentType>(test.Name).Should().Be(test.ExpectedValue);
            DocumentType.FromDatabaseValue(test.DatabaseValue).Should().Be(test.ExpectedValue);
        }

        documentTypes.Select(c => c.ExpectedValue).Should().BeEquivalentTo(GetAllDocumentType());
    }
}
