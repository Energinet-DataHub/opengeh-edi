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

using Energinet.DataHub.EDI.B2CWebApi.Models.ArchivedMeasureDataMessages;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.B2CWebApi.Mappers;

public static class MeasureDataDocumentTypeMapper
{
    private static readonly Dictionary<MeteringPointDocumentType, DocumentType> _measureDataDocumentTypeMappings = new()
    {
        { MeteringPointDocumentType.Acknowledgement, DocumentType.Acknowledgement },
        { MeteringPointDocumentType.NotifyValidatedMeasureData, DocumentType.NotifyValidatedMeasureData },
    };

    public static DocumentType ToDocumentType(MeteringPointDocumentType meteringPointDocumentType)
    {
        return _measureDataDocumentTypeMappings[meteringPointDocumentType];
    }

    public static MeteringPointDocumentType ToMeteringPointDocumentType(string documentTypeName)
    {
        return _measureDataDocumentTypeMappings
            .First(x => x.Value.ToString().Equals(documentTypeName, StringComparison.OrdinalIgnoreCase)).Key;
    }
}
