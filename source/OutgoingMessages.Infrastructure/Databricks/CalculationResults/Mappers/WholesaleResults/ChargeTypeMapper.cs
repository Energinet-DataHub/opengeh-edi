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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults.Mappers.WholesaleResults;

public static class ChargeTypeMapper
{
    public static ChargeType FromDeltaTableValue(string chargeType) =>
        chargeType switch
        {
            "fee" => ChargeType.Fee,
            "subscription" => ChargeType.Subscription,
            "tariff" => ChargeType.Tariff,

            _ => throw new ArgumentOutOfRangeException(
                nameof(chargeType),
                actualValue: chargeType,
                "Value does not contain a valid string representation of a charge type."),
        };

    public static string ToDeltaTableValue(ChargeType chargeType) =>
        chargeType switch
        {
            var ct when ct == ChargeType.Tariff => "tariff",
            var ct when ct == ChargeType.Fee => "fee",
            var ct when ct == ChargeType.Subscription => "subscription",
            _ => throw new ArgumentOutOfRangeException(
                nameof(chargeType),
                actualValue: chargeType,
                $"Cannot map ${nameof(ChargeType)} to delta table value"),
        };
}
