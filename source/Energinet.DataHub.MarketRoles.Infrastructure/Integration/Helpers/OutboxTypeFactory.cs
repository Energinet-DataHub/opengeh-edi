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
using Energinet.DataHub.MarketRoles.Infrastructure.EDI;
using Energinet.DataHub.MarketRoles.Infrastructure.Integration.IntegrationEvents.EnergySupplierChange;
using Energinet.DataHub.MarketRoles.Infrastructure.Integration.IntegrationEvents.FutureEnergySupplierChangeRegistered;

namespace Energinet.DataHub.MarketRoles.Infrastructure.Integration.Helpers
{
    public static class OutboxTypeFactory
    {
        private static readonly Dictionary<string, Type> _types = new()
        {
            { typeof(EnergySupplierChangedIntegrationEvent).FullName!, typeof(EnergySupplierChangedIntegrationEvent) },
            { typeof(FutureEnergySupplierChangeRegisteredIntegrationEvent).FullName!, typeof(FutureEnergySupplierChangeRegisteredIntegrationEvent) },
            { typeof(PostOfficeEnvelope).FullName!, typeof(PostOfficeEnvelope) },
        };

        public static Type GetType(string type)
        {
            return _types.TryGetValue(type, out var result)
                ? result
                : throw new ArgumentException("Outbox type is not implemented.");
        }
    }
}
