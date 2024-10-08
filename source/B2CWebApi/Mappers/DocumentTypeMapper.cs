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

namespace Energinet.DataHub.EDI.B2CWebApi.Mappers;

public static class DocumentTypeMapper
{
    private static readonly Dictionary<Models.DocumentType, string> DocumentTypeMappings = new()
    {
        { Models.DocumentType.NotifyAggregatedMeasureData, DocumentType.NotifyAggregatedMeasureData.Name },
        { Models.DocumentType.NotifyWholesaleServices, DocumentType.NotifyWholesaleServices.Name },
        { Models.DocumentType.RejectRequestAggregatedMeasureData, DocumentType.RejectRequestAggregatedMeasureData.Name },
        { Models.DocumentType.RejectRequestWholesaleSettlement, DocumentType.RejectRequestWholesaleSettlement.Name },
        { Models.DocumentType.RequestAggregatedMeasureData, IncomingDocumentType.RequestAggregatedMeasureData.Name },
        { Models.DocumentType.B2CRequestAggregatedMeasureData, IncomingDocumentType.B2CRequestAggregatedMeasureData.Name },
        { Models.DocumentType.RequestWholesaleSettlement, IncomingDocumentType.RequestWholesaleSettlement.Name },
        { Models.DocumentType.B2CRequestWholesaleSettlement, IncomingDocumentType.B2CRequestWholesaleSettlement.Name },
    };

    public static List<string> FromDocumentTypes(IReadOnlyCollection<Models.DocumentType>? documentTypes)
    {
        if (documentTypes == null) return [];
        return documentTypes.Select(dt => DocumentTypeMappings.TryGetValue(dt, out var name)
                ? name
                : throw new NotSupportedException($"Document type not supported: {dt}"))
            .ToList();
    }

    public static Models.DocumentType ToDocumentType(string documentType)
    {
        if (DocumentTypeMappings.ContainsValue(documentType))
        {
            return DocumentTypeMappings.First(x => x.Value == documentType).Key;
        }

        throw new NotSupportedException($"Document type not supported: {documentType}");
    }
}
