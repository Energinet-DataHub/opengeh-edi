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

public sealed class TotalMonthlyAmountWholesaleServicesDatabricksContract : IWholesaleServicesDatabricksContract
{
    private static string[] ColumnsToAggregateByForTotalMonthlyAmounts =>
[
    TotalMonthlyAmountsViewColumnNames.GridAreaCode,
        TotalMonthlyAmountsViewColumnNames.EnergySupplierId,
        TotalMonthlyAmountsViewColumnNames.ChargeOwnerId,
    ];

    private static string[] ColumnsToProjectForTotalMonthlyAmounts =>
    [
        TotalMonthlyAmountsViewColumnNames.CalculationId,
        TotalMonthlyAmountsViewColumnNames.CalculationType,
        TotalMonthlyAmountsViewColumnNames.CalculationVersion,
        TotalMonthlyAmountsViewColumnNames.CalculationResultId,
        TotalMonthlyAmountsViewColumnNames.GridAreaCode,
        TotalMonthlyAmountsViewColumnNames.EnergySupplierId,
        TotalMonthlyAmountsViewColumnNames.ChargeOwnerId,
        TotalMonthlyAmountsViewColumnNames.Currency,
        TotalMonthlyAmountsViewColumnNames.Time,
        TotalMonthlyAmountsViewColumnNames.Amount,
    ];

    public AmountType GetAmountType()
    {
        return AmountType.TotalMonthlyAmount;
    }

    public string GetSource(DeltaTableOptions tableOptions)
    {
        return
            $"{tableOptions.CalculationResultViewsSource}.{tableOptions.TOTAL_MONTHLY_AMOUNTS_V1_VIEW_NAME}";
    }

    public string GetCalculationTypeColumnName()
    {
        return TotalMonthlyAmountsViewColumnNames.CalculationType;
    }

    public string GetGridAreaCodeColumnName()
    {
        return TotalMonthlyAmountsViewColumnNames.GridAreaCode;
    }

    public string GetTimeColumnName()
    {
        return TotalMonthlyAmountsViewColumnNames.Time;
    }

    public string GetEnergySupplierIdColumnName()
    {
        return TotalMonthlyAmountsViewColumnNames.EnergySupplierId;
    }

    public string GetChargeOwnerIdColumnName()
    {
        return TotalMonthlyAmountsViewColumnNames.ChargeOwnerId;
    }

    public string GetChargeCodeColumnName()
    {
        throw new InvalidOperationException("Oh dear, there is no charge code for total monthly amounts");
    }

    public string GetChargeTypeColumnName()
    {
        throw new InvalidOperationException("Oh dear, there is no charge type for total monthly amounts");
    }

    public string GetCalculationVersionColumnName()
    {
        return TotalMonthlyAmountsViewColumnNames.CalculationVersion;
    }

    public string GetCalculationIdColumnName()
    {
        return TotalMonthlyAmountsViewColumnNames.CalculationId;
    }

    public string GetResolutionColumnName()
    {
        throw new InvalidOperationException("There is no resolution for total monthly amounts");
    }

    public string GetIsTaxColumnName()
    {
        throw new InvalidOperationException("Oh dear, there is no is_tax for total monthly amounts");
    }

    public string[] GetColumnsToProject()
    {
        return ColumnsToProjectForTotalMonthlyAmounts;
    }

    public string[] GetColumnsToAggregateBy()
    {
        return ColumnsToAggregateByForTotalMonthlyAmounts;
    }
}
