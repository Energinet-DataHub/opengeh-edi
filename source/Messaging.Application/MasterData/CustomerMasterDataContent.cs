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

using System;
using System.Collections.Generic;
using Messaging.Application.OutgoingMessages.CharacteristicsOfACustomerAtAnAp;

namespace Messaging.Application.MasterData;

public class CustomerMasterDataContent
{
    public CustomerMasterDataContent(Address address, bool electricalHeating, DateTime electricalHeatingStart, string firstCustomerId, string firstCustomerName, string secondCustomerId, string secondCustomerName, bool protectedName, bool hasEnergySupplier, DateTime supplyStart, IEnumerable<UsagePointLocation> usagePoints)
    {
        Address = address;
        ElectricalHeating = electricalHeating;
        ElectricalHeatingStart = electricalHeatingStart;
        FirstCustomerId = firstCustomerId;
        FirstCustomerName = firstCustomerName;
        SecondCustomerId = secondCustomerId;
        SecondCustomerName = secondCustomerName;
        ProtectedName = protectedName;
        HasEnergySupplier = hasEnergySupplier;
        SupplyStart = supplyStart;
        UsagePoints = usagePoints;
    }

    public Address Address { get; }

    public bool ElectricalHeating { get; }

    public DateTime ElectricalHeatingStart { get; }

    public string FirstCustomerId { get; }

    public string FirstCustomerName { get; }

    public string SecondCustomerId { get; }

    public string SecondCustomerName { get; }

    public bool ProtectedName { get; }

    public bool HasEnergySupplier { get; }

    public DateTime SupplyStart { get; }

    public IEnumerable<UsagePointLocation> UsagePoints { get; }
}
