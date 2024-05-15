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

using System.Collections.Generic;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;

namespace Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.EventProcessors;

public static class AmountPerChargeResultProducedProcessorExtensions
{
    public static IReadOnlyCollection<AmountPerChargeResultProducedV1.Types.MeteringPointType> SupportedMeteringPointTypes() =>
        new List<AmountPerChargeResultProducedV1.Types.MeteringPointType>
        {
            /* Metering point types */
            AmountPerChargeResultProducedV1.Types.MeteringPointType.Production,
            AmountPerChargeResultProducedV1.Types.MeteringPointType.Consumption,

            /* Child metering point types */
            AmountPerChargeResultProducedV1.Types.MeteringPointType.VeProduction, // D01: VE produktion (andel) (VE)
            AmountPerChargeResultProducedV1.Types.MeteringPointType.NetProduction, // D05: Nettoproduktion (M1)
            AmountPerChargeResultProducedV1.Types.MeteringPointType.SupplyToGrid, // D06: Leveret til net (M2)
            AmountPerChargeResultProducedV1.Types.MeteringPointType.ConsumptionFromGrid, // D07: Forbrug fra net (M3)
            AmountPerChargeResultProducedV1.Types.MeteringPointType.WholesaleServicesInformation, // D08: Afregningsgrundlag/Information
            AmountPerChargeResultProducedV1.Types.MeteringPointType.OwnProduction, // D09: Egenproduktion (EP)
            AmountPerChargeResultProducedV1.Types.MeteringPointType.NetFromGrid, // D10: Netto fra net (NFN)
            AmountPerChargeResultProducedV1.Types.MeteringPointType.NetToGrid, // D11: Netto til net (NTN)
            AmountPerChargeResultProducedV1.Types.MeteringPointType.TotalConsumption, // D12: Brutto forbrug (BF)
            AmountPerChargeResultProducedV1.Types.MeteringPointType.ElectricalHeating, // D14: Elvarme
            AmountPerChargeResultProducedV1.Types.MeteringPointType.NetConsumption, // D15: Nettoforbrug
            AmountPerChargeResultProducedV1.Types.MeteringPointType.EffectSettlement, // D19: Effektbetaling
        };
}
