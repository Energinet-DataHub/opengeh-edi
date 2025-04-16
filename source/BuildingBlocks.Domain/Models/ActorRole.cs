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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Exceptions;
using PMCoreTypes = Energinet.DataHub.ProcessManager.Abstractions.Core.ValueObjects;

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

[Serializable]
public class ActorRole : DataHubType<ActorRole>
{
    public static readonly ActorRole MeteringPointAdministrator = new(PMCoreTypes.ActorRole.MeteringPointAdministrator.Name, "DDZ", 1);
    public static readonly ActorRole EnergySupplier = new(PMCoreTypes.ActorRole.EnergySupplier.Name, "DDQ", 2);

    // A grid operator has two roles.
    // GridOperator (DDM) when creating a new metering point
    public static readonly ActorRole GridAccessProvider = new(PMCoreTypes.ActorRole.GridAccessProvider.Name, "DDM", 3);
    public static readonly ActorRole MeteredDataAdministrator = new(PMCoreTypes.ActorRole.MeteredDataAdministrator.Name, "DGL", 4);

    // A grid operator has two roles.
    // MeteredDataResponsible (MDR) when requesting data from DataHub
    public static readonly ActorRole MeteredDataResponsible = new(PMCoreTypes.ActorRole.MeteredDataResponsible.Name, "MDR", 5);
    public static readonly ActorRole BalanceResponsibleParty = new(PMCoreTypes.ActorRole.BalanceResponsibleParty.Name, "DDK", 6);

    public static readonly ActorRole ImbalanceSettlementResponsible = new(PMCoreTypes.ActorRole.ImbalanceSettlementResponsible.Name, "DDX", 7);
    public static readonly ActorRole SystemOperator = new(PMCoreTypes.ActorRole.SystemOperator.Name, "EZ", 8);
    public static readonly ActorRole DanishEnergyAgency = new(PMCoreTypes.ActorRole.DanishEnergyAgency.Name, "STS", 9);
    public static readonly ActorRole Delegated = new(PMCoreTypes.ActorRole.Delegated.Name, "DEL", 10);

    // DataHubAdministrator is a special role that is used to indicate that the user has special permissions.
    public static readonly ActorRole DataHubAdministrator = new(PMCoreTypes.ActorRole.DataHubAdministrator.Name, string.Empty, 11);

    /// <summary>
    /// Represents the actor role in the Edi system.
    /// </summary>
    /// <remarks>
    /// databaseValue must be unique for each actor role and
    /// changing the byte value of an existing role is not allowed, as it would break the mapping.
    /// </remarks>
    /// <param name="name"></param>
    /// <param name="code"></param>
    /// <param name="databaseValue">Represents the actor role in the database as a byte for database efficiency.</param>
    [JsonConstructor]
    private ActorRole(string name, string code, byte databaseValue)
        : base(name, code)
    {
        DatabaseValue = databaseValue;
    }

    /// <summary>
    /// Each actor role is assigned a unique byte value, allowing for efficient storage and retrieval.
    /// It ensures consistency and performance while mapping roles to database values and application logic.
    /// Creating a new actor role must be done with caution to avoid conflicts with existing roles.
    /// Changing the byte value of an existing role is not allowed, as it would break the mapping.
    /// </summary>
    public byte DatabaseValue { get; }

    public static ActorRole Create(PMCoreTypes.ActorRole actorNumber)
    {
        ArgumentNullException.ThrowIfNull(actorNumber);
        return FromName(actorNumber.Name) ?? throw InvalidActorNumberException.Create(actorNumber.Name);
    }

    public static ActorRole FromDatabaseValue(byte databaseValue)
    {
        return GetAll<ActorRole>().FirstOrDefault(t => t.DatabaseValue == databaseValue)
               ?? throw new InvalidOperationException(
                   $"{databaseValue} is not a valid {nameof(ActorRole)} {nameof(databaseValue)}");
    }

    public override string ToString()
    {
        return Name;
    }

    /// <summary>
    /// The ActorRole for a ActorMessageQueue. This is implemented to support the "hack" where
    ///     a MeteredDataResponsible uses the GridOperator queue
    /// Is used when peeking, dequeuing and enqueuing
    /// </summary>
    public ActorRole ForActorMessageQueue()
    {
        return HackForMeteredDataResponsible();
    }

    /// <summary>
    /// The ActorRole for a ActorMessageDelegation. This is implemented to support the "hack" where
    ///     a MeteredDataResponsible uses the GridOperator
    /// </summary>
    public ActorRole ForActorMessageDelegation()
    {
        return HackForMeteredDataResponsible();
    }

    public ProcessManager.Abstractions.Core.ValueObjects.ActorRole ToProcessManagerActorRole()
    {
        return ProcessManager.Abstractions.Core.ValueObjects.ActorRole.FromName(Name);
    }

    private ActorRole HackForMeteredDataResponsible()
    {
        if (WorkaroundFlags.MeteredDataResponsibleToGridOperatorHack && Equals(MeteredDataResponsible))
            return GridAccessProvider;

        return this;
    }
}
