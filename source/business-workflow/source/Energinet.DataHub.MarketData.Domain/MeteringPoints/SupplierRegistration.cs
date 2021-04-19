// Copyright 2020 Energinet DataHub A/S
//
// Licensed under the Apache License, Version 2.0 (the "License2");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using Energinet.DataHub.MarketData.Domain.BusinessProcesses;
using Energinet.DataHub.MarketData.Domain.SeedWork;
using NodaTime;

namespace Energinet.DataHub.MarketData.Domain.MeteringPoints
{
    internal class SupplierRegistration : Entity
    {
        public SupplierRegistration(EnergySupplierId energySupplierId, ProcessId processId)
        {
            EnergySupplierId = energySupplierId;
            ProcessId = processId;
        }

        private SupplierRegistration(EnergySupplierId energySupplierId, Instant? startOfSupplyDate, Instant? endOfSupplyDate, ProcessId processId)
        {
            EnergySupplierId = energySupplierId;
            StartOfSupplyDate = startOfSupplyDate;
            EndOfSupplyDate = endOfSupplyDate;
            ProcessId = processId;
        }

        public EnergySupplierId EnergySupplierId { get; }

        public Instant? StartOfSupplyDate { get; private set; } = null;

        public Instant? EndOfSupplyDate { get; } = null;

        public ProcessId ProcessId { get; }

        public static SupplierRegistration CreateFrom(SupplierRegistrationSnapshot snapshot)
        {
            return new SupplierRegistration(
                new EnergySupplierId(snapshot.EnergySupplierId),
                snapshot.SupplyStartDate,
                snapshot.EndOfSupplyDate,
                new ProcessId(snapshot.ProcessId));
        }

        public void StartOfSupply(Instant supplyStartDate)
        {
            StartOfSupplyDate = supplyStartDate;
        }

        public SupplierRegistrationSnapshot GetSnapshot()
        {
            return new SupplierRegistrationSnapshot(EnergySupplierId.Value, StartOfSupplyDate, EndOfSupplyDate, ProcessId.Value !);
        }
    }
}
