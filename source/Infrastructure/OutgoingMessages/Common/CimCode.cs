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
using Domain.Actors;
using Domain.OutgoingMessages;

namespace Infrastructure.OutgoingMessages.Common;

public static class CimCode
{
    public static string Of(ProcessType processType)
    {
        ArgumentNullException.ThrowIfNull(processType);

        if (processType == ProcessType.BalanceFixing)
            return "D04";

        if (processType == ProcessType.MoveIn)
            return "E65";

        throw new InvalidOperationException($"No code has been defined for {processType.Name}");
    }

    public static string Of(MeteringPointType meteringPointType)
    {
        ArgumentNullException.ThrowIfNull(meteringPointType);

        if (meteringPointType == MeteringPointType.Consumption)
            return "E17";

        if (meteringPointType == MeteringPointType.Production)
            return "E18";

        throw new InvalidOperationException($"No code has been defined for {meteringPointType.Name}");
    }

    public static string Of(MarketRole marketRole)
    {
        ArgumentNullException.ThrowIfNull(marketRole);

        if (marketRole == MarketRole.EnergySupplier)
            return "DDQ";
        if (marketRole == MarketRole.GridOperator)
            return "DDM";
        if (marketRole == MarketRole.MeteredDataResponsible)
            return "MDR";
        if (marketRole == MarketRole.MeteringDataAdministrator)
            return "DGL";
        if (marketRole == MarketRole.MeteringPointAdministrator)
            return "DDZ";
        if (marketRole == MarketRole.BalanceResponsible)
            return "DDK";

        throw new InvalidOperationException($"No code has been defined for {marketRole.Name}");
    }
}
