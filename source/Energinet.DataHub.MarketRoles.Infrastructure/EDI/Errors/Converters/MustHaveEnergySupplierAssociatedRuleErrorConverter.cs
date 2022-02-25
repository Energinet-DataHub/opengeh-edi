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
using Energinet.DataHub.MarketRoles.Application.EDI;
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints.Rules.ChangeEnergySupplier;

namespace Energinet.DataHub.MarketRoles.Infrastructure.EDI.Errors.Converters
{
    public class MustHaveEnergySupplierAssociatedRuleErrorConverter : ErrorConverter<MustHaveEnergySupplierAssociatedRuleError>
    {
        protected override ErrorMessage Convert([NotNull] MustHaveEnergySupplierAssociatedRuleError validationError)
        {
            return new("TODO1", $"Can't change energy supplier if no energy supplier is already present on metering point.");
        }
    }
}
