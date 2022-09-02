using System;
using System.Collections.Generic;
using Messaging.Application.OutgoingMessages.CharacteristicsOfACustomerAtAnAp;
using NodaTime;

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
