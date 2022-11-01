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

using Messaging.Domain.SeedWork;
using NodaTime;

namespace Messaging.Domain.Transactions.MoveIn;

public class CustomerMasterData : ValueObject
{
    public CustomerMasterData(
        string marketEvaluationPoint,
        bool electricalHeating,
        Instant? electricalHeatingStart,
        string firstCustomerId,
        string firstCustomerName,
        string? secondCustomerId,
        string? secondCustomerName,
        bool protectedName,
        bool hasEnergySupplier,
        Instant supplyStart)
    {
        MarketEvaluationPoint = marketEvaluationPoint;
        ElectricalHeating = electricalHeating;
        ElectricalHeatingStart = electricalHeatingStart;
        FirstCustomerId = firstCustomerId;
        FirstCustomerName = firstCustomerName;
        SecondCustomerId = secondCustomerId;
        SecondCustomerName = secondCustomerName;
        ProtectedName = protectedName;
        HasEnergySupplier = hasEnergySupplier;
        SupplyStart = supplyStart;
    }

    public string MarketEvaluationPoint { get; }

    public bool ElectricalHeating { get; }

    public Instant? ElectricalHeatingStart { get; }

    public string FirstCustomerId { get; }

    public string FirstCustomerName { get; }

    public string? SecondCustomerId { get; }

    public string? SecondCustomerName { get; }

    public bool ProtectedName { get; }

    public bool HasEnergySupplier { get; }

    public Instant SupplyStart { get; }
}
