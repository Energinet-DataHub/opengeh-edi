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

using Energinet.DataHub.EDI.ArchivedMessages.Interfaces.Models;

namespace Energinet.DataHub.EDI.B2CWebApi.Mappers;

public static class FieldToSortByMapper
{
    public static FieldToSortBy? MapToFieldToSortBy(Energinet.DataHub.EDI.B2CWebApi.Models.FieldToSortBy? fieldToSortBy)
    {
        return fieldToSortBy switch
        {
            Models.FieldToSortBy.CreatedAt => FieldToSortBy.CreatedAt,
            Models.FieldToSortBy.MessageId => FieldToSortBy.MessageId,
            Models.FieldToSortBy.SenderNumber => FieldToSortBy.SenderNumber,
            Models.FieldToSortBy.ReceiverNumber => FieldToSortBy.ReceiverNumber,
            Models.FieldToSortBy.DocumentType => FieldToSortBy.DocumentType,
            _ => null,
        };
    }
}
