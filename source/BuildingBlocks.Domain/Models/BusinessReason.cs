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

using System.Text.Json.Serialization;
using PMTypes = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects;

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

public sealed class BusinessReason : DataHubType<BusinessReason>
{
    public static readonly BusinessReason MoveIn = new(PMTypes.BusinessReason.MoveIn.Name, "E65", 1);
    public static readonly BusinessReason BalanceFixing = new(PMTypes.BusinessReason.BalanceFixing.Name, "D04", 2);
    public static readonly BusinessReason PreliminaryAggregation = new(PMTypes.BusinessReason.PreliminaryAggregation.Name, "D03", 3);
    public static readonly BusinessReason WholesaleFixing = new(PMTypes.BusinessReason.WholesaleFixing.Name, "D05", 4);
    public static readonly BusinessReason Correction = new(PMTypes.BusinessReason.Correction.Name, "D32", 5);
    public static readonly BusinessReason PeriodicMetering = new(PMTypes.BusinessReason.PeriodicMetering.Name, "E23", 6);
    public static readonly BusinessReason PeriodicFlexMetering = new(PMTypes.BusinessReason.PeriodicFlexMetering.Name, "D42", 7);
    public static readonly BusinessReason ReminderOfMissingMeasurementLog = new(nameof(ReminderOfMissingMeasurementLog), "D26", 8);

    /// <summary>
    /// Represents the business reason in the Edi system.
    /// </summary>
    /// <remarks>
    /// databaseValue must be unique for each business reason and
    /// changing the byte value of an existing business reason is not allowed, as it would break the mapping.
    /// </remarks>
    /// <param name="name"></param>
    /// <param name="code"></param>
    /// <param name="databaseValue">Represents the business reason in the database as a byte for database efficiency.</param>
    [JsonConstructor]
    private BusinessReason(string name, string code, byte databaseValue)
     : base(name, code)
    {
        DatabaseValue = databaseValue;
    }

    /// <summary>
    /// Each business reason is assigned a unique byte value, allowing for efficient storage and retrieval.
    /// It ensures consistency and performance while mapping business reason to database values and application logic.
    /// Creating a new business reason must be done with caution to avoid conflicts with existing business reason.
    /// Changing the byte value of an existing business reason is not allowed, as it would break the mapping.
    /// </summary>
    public byte DatabaseValue { get; }

    public static BusinessReason FromDatabaseValue(byte databaseValue)
    {
        return GetAll<BusinessReason>().FirstOrDefault(t => t.DatabaseValue == databaseValue)
               ?? throw new InvalidOperationException(
                   $"{databaseValue} is not a valid {nameof(BusinessReason)} {nameof(databaseValue)}");
    }

    public ProcessManager.Components.Abstractions.ValueObjects.BusinessReason ToProcessManagerBusinessReason()
    {
        return ProcessManager.Components.Abstractions.ValueObjects.BusinessReason.FromName(Name);
    }
}
