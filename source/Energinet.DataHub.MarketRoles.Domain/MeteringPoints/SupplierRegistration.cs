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

using Energinet.DataHub.MarketRoles.Domain.EnergySuppliers;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;
using NodaTime;

namespace Energinet.DataHub.MarketRoles.Domain.MeteringPoints
{
    public class SupplierRegistration : Entity
    {
        public SupplierRegistration(EnergySupplierId energySupplierId, BusinessProcessId businessProcessId)
        {
            Id = SupplierRegistrationId.New();
            EnergySupplierId = energySupplierId;
            BusinessProcessId = businessProcessId;
        }

        private SupplierRegistration(EnergySupplierId energySupplierId, Instant? startOfSupplyDate, Instant? endOfSupplyDate, BusinessProcessId businessProcessId)
            : this(energySupplierId, businessProcessId)
        {
            EnergySupplierId = energySupplierId;
            StartOfSupplyDate = startOfSupplyDate;
            EndOfSupplyDate = endOfSupplyDate;
            BusinessProcessId = businessProcessId;
        }

        public SupplierRegistrationId Id { get; }

        public EnergySupplierId EnergySupplierId { get; }

        public Instant? StartOfSupplyDate { get; private set; }

        public Instant? EndOfSupplyDate { get; private set; }

        public BusinessProcessId BusinessProcessId { get; }

        public void StartOfSupply(Instant supplyStartDate)
        {
            StartOfSupplyDate = supplyStartDate;
        }

        public void MarkEndOfSupply(Instant endOfSupplyDate)
        {
            EndOfSupplyDate = endOfSupplyDate;
        }
    }
}
