﻿// Copyright 2020 Energinet DataHub A/S
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

using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults.WholesaleResults;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_026_028.BRS_028.V1.Model;
using Resolution = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects.Resolution;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.Mappers;

public static class AmountTypeMapper
{
    public static IReadOnlyCollection<AmountType> Map(
        Resolution? resolution,
        IReadOnlyCollection<RequestCalculatedWholesaleServicesAcceptedV1.AcceptedChargeType> chargeTypes)
    {
        if (resolution is null)
            return [AmountType.AmountPerCharge];

        if (resolution == Resolution.Monthly)
        {
            var isRequestForAllCharges = chargeTypes.Count == 0;
            return isRequestForAllCharges
                ? [AmountType.MonthlyAmountPerCharge, AmountType.TotalMonthlyAmount]
                : [AmountType.MonthlyAmountPerCharge];
        }

        // This case shouldn't be possible, since it should be enforced by our validation that the resolution
        // is either null for a Resolution.Month for monthly_amount_per_charge or null for a amount_per_charge
        if (resolution == Resolution.Hourly || resolution == Resolution.Daily || resolution == Resolution.QuarterHourly)
        {
            throw new ArgumentOutOfRangeException(
                nameof(resolution),
                resolution,
                $"{nameof(Resolution)} should be either {nameof(Resolution.Monthly)} or null while converting to {nameof(AmountType)}");
        }

        throw new ArgumentOutOfRangeException(
            nameof(resolution),
            resolution,
            $"Unknown {nameof(Resolution)} value while converting to {nameof(AmountType)}");
    }
}
