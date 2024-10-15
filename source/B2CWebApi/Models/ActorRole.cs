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

namespace Energinet.DataHub.EDI.B2CWebApi.Models;

public enum ActorRole
{
     MeteringPointAdministrator = 0,
     EnergySupplier = 1,
     GridAccessProvider = 2,
     MeteredDataAdministrator = 3,
     MeteredDataResponsible = 4,
     BalanceResponsibleParty = 5,
     ImbalanceSettlementResponsible = 6,
     SystemOperator = 7,
     DanishEnergyAgency = 8,
     Delegated = 9,
     DataHubAdministrator = 10,
}
