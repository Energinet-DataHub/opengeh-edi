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

using Energinet.DataHub.MarketRoles.Domain.SeedWork;

namespace Energinet.DataHub.MarketRoles.Domain.Actors
{
    public class Role : EnumerationType
    {
        public static readonly Role None = new Role(0, nameof(None));
        public static readonly Role MeteringPointAdministrator = new Role(1, nameof(MeteringPointAdministrator));
        public static readonly Role GridAccessProvider = new Role(2, nameof(GridAccessProvider));
        public static readonly Role BalancePowerSupplier = new Role(3, nameof(BalancePowerSupplier));
        public static readonly Role SystemOperator = new Role(4, nameof(SystemOperator));
        public static readonly Role MeteredDataResponsible = new Role(5, nameof(MeteredDataResponsible));

        public Role(int id, string name)
            : base(id, name)
        {
        }
    }
}
