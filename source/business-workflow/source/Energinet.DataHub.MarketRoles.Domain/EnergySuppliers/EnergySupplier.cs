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
using Energinet.DataHub.MarketRoles.Domain.SeedWork;

namespace Energinet.DataHub.MarketRoles.Domain.EnergySuppliers
{
    public class EnergySupplier : AggregateRootBase
    {
        public EnergySupplier(EnergySupplierId energySupplierId, GlnNumber glnNumber)
            : base()
        {
            EnergySupplierId = energySupplierId ?? throw new ArgumentNullException(nameof(energySupplierId));
            GlnNumber = glnNumber ?? throw new ArgumentNullException(nameof(glnNumber));
        }

#pragma warning disable 8618
        private EnergySupplier()
#pragma warning restore 8618
        {
            // EF core
        }

        public EnergySupplierId EnergySupplierId { get; }

        public GlnNumber GlnNumber { get; }

        public EnergySupplierSnapshot GetSnapshot()
        {
            return new EnergySupplierSnapshot(Guid.Empty, GlnNumber.Value, Version);
        }
    }
}
