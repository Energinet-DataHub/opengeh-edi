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

using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults.DeltaTableConstants;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Extensions.Options;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults.WholesaleResults;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults.Statements;

public sealed class AmountsPerChargeWholesaleServicesDatabricksContract : IWholesaleServicesDatabricksContract
{
    private static string[] ColumnsToAggregateByForAmountsPerCharge =>
[
    AmountsPerChargeViewColumnNames.GridAreaCode,
        AmountsPerChargeViewColumnNames.EnergySupplierId,
        AmountsPerChargeViewColumnNames.ChargeOwnerId,
        AmountsPerChargeViewColumnNames.ChargeType,
        AmountsPerChargeViewColumnNames.ChargeCode,
        AmountsPerChargeViewColumnNames.Resolution,
        AmountsPerChargeViewColumnNames.MeteringPointType,
        AmountsPerChargeViewColumnNames.SettlementMethod,
    ];

    private static string[] ColumnsToProjectForAmountsPerCharge =>
    [
        AmountsPerChargeViewColumnNames.CalculationId,
        AmountsPerChargeViewColumnNames.CalculationType,
        AmountsPerChargeViewColumnNames.CalculationVersion,
        AmountsPerChargeViewColumnNames.CalculationResultId,
        AmountsPerChargeViewColumnNames.GridAreaCode,
        AmountsPerChargeViewColumnNames.EnergySupplierId,
        AmountsPerChargeViewColumnNames.ChargeCode,
        AmountsPerChargeViewColumnNames.ChargeType,
        AmountsPerChargeViewColumnNames.ChargeOwnerId,
        AmountsPerChargeViewColumnNames.Resolution,
        AmountsPerChargeViewColumnNames.QuantityUnit,
        AmountsPerChargeViewColumnNames.MeteringPointType,
        AmountsPerChargeViewColumnNames.SettlementMethod,
        AmountsPerChargeViewColumnNames.IsTax,
        AmountsPerChargeViewColumnNames.Currency,
        AmountsPerChargeViewColumnNames.Time,
        AmountsPerChargeViewColumnNames.Quantity,
        AmountsPerChargeViewColumnNames.QuantityQualities,
        AmountsPerChargeViewColumnNames.Price,
        AmountsPerChargeViewColumnNames.Amount,
    ];

    public AmountType GetAmountType()
    {
        return AmountType.AmountPerCharge;
    }

    public string GetSource(DeltaTableOptions tableOptions)
    {
        return $"{tableOptions.CalculationResultViewsSource}.{tableOptions.AMOUNTS_PER_CHARGE_V1_VIEW_NAME}";
    }

    public string GetCalculationTypeColumnName()
    {
        return AmountsPerChargeViewColumnNames.CalculationType;
    }

    public string GetGridAreaCodeColumnName()
    {
        return AmountsPerChargeViewColumnNames.GridAreaCode;
    }

    public string GetTimeColumnName()
    {
        return AmountsPerChargeViewColumnNames.Time;
    }

    public string GetEnergySupplierIdColumnName()
    {
        return AmountsPerChargeViewColumnNames.EnergySupplierId;
    }

    public string GetChargeOwnerIdColumnName()
    {
        return AmountsPerChargeViewColumnNames.ChargeOwnerId;
    }

    public string GetChargeCodeColumnName()
    {
        return AmountsPerChargeViewColumnNames.ChargeCode;
    }

    public string GetChargeTypeColumnName()
    {
        return AmountsPerChargeViewColumnNames.ChargeType;
    }

    public string GetCalculationVersionColumnName()
    {
        return AmountsPerChargeViewColumnNames.CalculationVersion;
    }

    public string GetCalculationIdColumnName()
    {
        return AmountsPerChargeViewColumnNames.CalculationId;
    }

    public string GetResolutionColumnName()
    {
        return AmountsPerChargeViewColumnNames.Resolution;
    }

    public string GetIsTaxColumnName()
    {
        return AmountsPerChargeViewColumnNames.IsTax;
    }

    public string[] GetColumnsToProject()
    {
        return ColumnsToProjectForAmountsPerCharge;
    }

    public string[] GetColumnsToAggregateBy()
    {
        return ColumnsToAggregateByForAmountsPerCharge;
    }
}
