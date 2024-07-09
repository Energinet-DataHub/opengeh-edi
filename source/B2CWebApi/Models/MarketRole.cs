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

using System.Diagnostics.CodeAnalysis;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.B2CWebApi.Models;

[SuppressMessage("Usage", "CA1034", Justification = "Nested types should not be visible")]
public class MarketRole : DataHubType<MarketRole>
{
    public static readonly MarketRole CalculationResponsibleRole = new("DGL", "CalculationResponsible");
    public static readonly MarketRole EnergySupplier = new("DDQ", "EnergySupplier");
    public static readonly MarketRole MeteredDataResponsible = new("MDR", "MeteredDataResponsible");
    public static readonly MarketRole BalanceResponsibleParty = new("DDK", "BalanceResponsibleParty");
    public static readonly MarketRole GridAccessProvider = new("DDM", "GridAccessProvider");
    public static readonly MarketRole SystemOperator = new("EZ", "SystemOperator");

    private MarketRole(string code, string name)
        : base(name, code)
    {
    }
}
