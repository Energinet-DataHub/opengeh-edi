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

using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Queries;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.OutgoingMessages.TestData;

/// <summary>
/// Test data description for scenario using the view described by <see cref="EnergyResultPerEnergySupplierBrpGridAreaQuery"/>.
/// </summary>
public class EnergyResultPerEnergySupplierBrpGridAreaDescription
    : EnergyResultTestDataDescription
{
    public EnergyResultPerEnergySupplierBrpGridAreaDescription()
        : base("balance_fixing_01-11-2022_01-12-2022_ga_543_per_es_brp_ga_v1.csv")
    {
    }

    public override Guid CalculationId => Guid.Parse("a8cfe7c7-f197-405c-b922-52153fa0332d");

    public override string GridAreaCode => "543";

    public override int ExpectedOutgoingMessagesCount => 35 * 2; // 35 results, which we must prepare for BRP and ES
}
