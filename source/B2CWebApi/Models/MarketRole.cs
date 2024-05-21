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

namespace Energinet.DataHub.EDI.B2CWebApi.Models;

[SuppressMessage("Usage", "CA1034", Justification = "Nested types should not be visible")]
public static class MarketRole
{
    public static class CalculationResponsibleRole
    {
        public const string Code = "DGL";
        public const string Name = "CalculationResponsible";
    }

    public static class EnergySupplier
    {
        public const string Code = "DDQ";
        public const string Name = "EnergySupplier";
    }

    public static class MeteredDataResponsible
    {
        public const string Code = "MDR";
        public const string Name = "MeteredDataResponsible";
    }

    public static class BalanceResponsibleParty
    {
        public const string Code = "DDK";
        public const string Name = "BalanceResponsibleParty";
    }

    public static class GridAccessProvider
    {
        public const string Code = "DDM";
        public const string Name = "GridAccessProvider";
    }

    public static class SystemOperator
    {
        public const string Code = "EZ";
        public const string Name = "SystemOperator";
    }
}
