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
using Energinet.DataHub.MarketRoles.Domain.EnergySuppliers;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;
using NodaTime;

namespace Energinet.DataHub.MarketRoles.Domain.MeteringPoints.Events
{
    public class EnergySupplierChanged : DomainEventBase
    {
        public EnergySupplierChanged(Guid accountingPointId, string gsrnNumber, Guid businessProcessId, string transaction, Guid energySupplierId, Instant startOfSupplyDate)
        {
            AccountingPointId = accountingPointId;
            GsrnNumber = gsrnNumber;
            BusinessProcessId = businessProcessId;
            Transaction = transaction;
            EnergySupplierId = energySupplierId;
            StartOfSupplyDate = startOfSupplyDate;
        }

        public string GsrnNumber { get; }

        public Guid AccountingPointId { get; }

        public Guid BusinessProcessId { get; }

        public string Transaction { get; }

        public Guid EnergySupplierId { get; }

        public Instant StartOfSupplyDate { get; }
    }
}
