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

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

public class DocumentType : EnumerationType
{
    public static readonly DocumentType RequestAggregatedMeasureData = new(nameof(RequestAggregatedMeasureData), MessageCategory.Aggregations, 1);
    public static readonly DocumentType NotifyAggregatedMeasureData = new(nameof(NotifyAggregatedMeasureData), MessageCategory.Aggregations, 2);
    public static readonly DocumentType RejectRequestAggregatedMeasureData = new(nameof(RejectRequestAggregatedMeasureData), MessageCategory.Aggregations, 3);

    public static readonly DocumentType RequestWholesaleSettlement = new(nameof(RequestWholesaleSettlement), MessageCategory.Aggregations, 4);
    public static readonly DocumentType NotifyWholesaleServices = new(nameof(NotifyWholesaleServices), MessageCategory.Aggregations, 5);
    public static readonly DocumentType RejectRequestWholesaleSettlement = new(nameof(RejectRequestWholesaleSettlement), MessageCategory.Aggregations, 6);

    public static readonly DocumentType NotifyValidatedMeasureData = new(nameof(NotifyValidatedMeasureData), MessageCategory.MeasureData, 7);
    public static readonly DocumentType Acknowledgement = new(nameof(Acknowledgement), MessageCategory.MeasureData, 8);

    /// <summary>
    /// Represents the document type in the Edi system.
    /// </summary>
    /// <remarks>
    /// databaseValue must be unique for each document type and
    /// changing the byte value of an existing document type is not allowed, as it would break the mapping.
    /// </remarks>
    /// <param name="name"></param>
    /// <param name="category"></param>
    /// <param name="databaseValue">Represents the document type in the database as a byte for database efficiency.</param>
    private DocumentType(string name, MessageCategory category, byte databaseValue)
        : base(name)
    {
        Category = category;
        DatabaseValue = databaseValue;
    }

    public MessageCategory Category { get; }

    public byte DatabaseValue { get; }

    /// <summary>
    /// Each document type is assigned a unique byte value, allowing for efficient storage and retrieval.
    /// It ensures consistency and performance while mapping document type to database values and application logic.
    /// Creating a new document type must be done with caution to avoid conflicts with existing document type.
    /// Changing the byte value of an existing role is not allowed, as it would break the mapping.
    /// </summary>
    public static DocumentType FromDatabaseValue(byte databaseValue)
    {
        return GetAll<DocumentType>().FirstOrDefault(t => t.DatabaseValue == databaseValue)
               ?? throw new InvalidOperationException(
                   $"{databaseValue} is not a valid {nameof(DocumentType)} {nameof(databaseValue)}");
    }

    public override string ToString()
    {
        return Name;
    }
}
