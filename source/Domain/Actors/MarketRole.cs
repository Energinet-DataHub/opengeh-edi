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

using Energinet.DataHub.EDI.Domain.Common;

namespace Energinet.DataHub.EDI.Domain.Actors;

public class MarketRole : EnumerationType
{
    public static readonly MarketRole MeteringPointAdministrator = new(0, "MeteringPointAdministrator", "DDZ");
    public static readonly MarketRole EnergySupplier = new(1, "EnergySupplier", "DDQ");

    // A grid operator has two roles.
    // GridOperator (DDM) when creating a new metering point
    public static readonly MarketRole GridOperator = new(2, "GridOperator", "DDM");
    public static readonly MarketRole MeteringDataAdministrator = new(3, "MeteringDataAdministrator", string.Empty);

    // A grid operator has two roles.
    // MeteredDataResponsible (MDR) when requesting data from DataHub
    public static readonly MarketRole MeteredDataResponsible = new(4, "MeteredDataResponsible", "MDR");
    public static readonly MarketRole BalanceResponsibleParty = new(5, "BalanceResponsibleParty", "DDK");

    public static readonly MarketRole CalculationResponsibleRole = new(5, "CalculationResponsibleRole", "DGL");
    public static readonly MarketRole MasterDataResponsibleRole = new(6, "MasterDataResponsibleRole", "DDZ");

    private MarketRole(int id, string name, string code)
        : base(id, name)
    {
        Code = code;
    }

    public string Code { get; }

    public static MarketRole FromCode(string code)
    {
        var matchingItem = GetAll<MarketRole>().FirstOrDefault(item => item.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
        return matchingItem ?? throw new InvalidOperationException($"'{code}' is not a valid code in {typeof(MarketRole)}");
    }

    public static MarketRole FromName(string name)
    {
        var matchingItem = GetAll<MarketRole>().FirstOrDefault(item => item.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        return matchingItem ?? throw new InvalidOperationException($"'{name}' is not a valid code in {typeof(MarketRole)}");
    }

    public override string ToString()
    {
        return Name;
    }
}
