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
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableMappers;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults.WholesaleResults;
using ChargeTypeMapper = Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults.Mappers.WholesaleResults.ChargeTypeMapper;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults.Statements;

public sealed class WholesaleServicesQuerySnippetProvider(
    IWholesaleServicesDatabricksContract databricksContract,
    WholesaleServicesQueryParameters queryParameters)
{
    private const string SystemOperatorActorNumber = "5790000432752";
    private readonly WholesaleServicesQueryParameters _queryParameters = queryParameters;

    public IWholesaleServicesDatabricksContract DatabricksContract { get; } = databricksContract;

    internal string GetProjection(string prefix)
    {
        return string.Join(", ", DatabricksContract.GetColumnsToProject().Select(ctp => $"`{prefix}`.`{ctp}`"));
    }

    internal string GetOrdering(string prefix)
    {
        return $"""
                {string.Join(", ", DatabricksContract.GetColumnsToAggregateBy().Select(ctab => $"{prefix}.{ctab}"))}, {prefix}.{DatabricksContract.GetTimeColumnName()}
                """;
    }

    internal string GetSelection(string table = "wrv")
    {
        var sql = $"""
                   ({table}.{DatabricksContract.GetTimeColumnName()} >= '{_queryParameters.Period.Start}'
                     AND {table}.{DatabricksContract.GetTimeColumnName()} < '{_queryParameters.Period.End}')
                   """;

        if (_queryParameters.GridAreaCodes.Count != 0)
        {
            sql += $"""
                    AND {table}.{DatabricksContract.GetGridAreaCodeColumnName()} in ({string.Join(',', _queryParameters.GridAreaCodes.Select(gridAreaCode => $"'{gridAreaCode}'"))})
                    """;
        }

        sql = _queryParameters.AmountType != AmountType.TotalMonthlyAmount
            ? GetActorsForNonTotalAmountsSelection(table, sql)
            : GetActorsForTotalAmountsSelection(table, sql);

        if (_queryParameters.ChargeTypes.Count != 0)
        {
            var chargeTypesSql = _queryParameters.ChargeTypes
                .Select<(string? ChargeCode, ChargeType? ChargeType), string>(c =>
                    GetChargeTypeSelection(c.ChargeCode, c.ChargeType, table))
                .ToList();

            sql += $"""
                    AND ({string.Join(" OR ", chargeTypesSql)})
                    """;
        }

        return sql;
    }

    internal string GetLatestOrFixedCalculationTypeSelection(
        string prefix,
        IReadOnlyCollection<CalculationTypeForGridArea> calculationTypePerGridAreas)
    {
        if (!IsQueryForLatestCorrection())
        {
            return $"""
                    {prefix}.{DatabricksContract.GetCalculationTypeColumnName()} = '{CalculationTypeMapper.ToDeltaTableValue(_queryParameters.BusinessReason, _queryParameters.SettlementVersion)}'
                    """;
        }

        if (calculationTypePerGridAreas.Count <= 0)
        {
            return """
                   FALSE
                   """;
        }

        var calculationTypePerGridAreaConstraints = calculationTypePerGridAreas
            .Select(ctpga => $"""
                              ({prefix}.{DatabricksContract.GetGridAreaCodeColumnName()} = '{ctpga.GridArea}' AND {prefix}.{DatabricksContract.GetCalculationTypeColumnName()} = '{ctpga.CalculationType}')
                              """);

        return $"""
                ({string.Join(" OR ", calculationTypePerGridAreaConstraints)})
                """;
    }

    private bool IsQueryForLatestCorrection()
    {
        return _queryParameters.BusinessReason == BusinessReason.Correction
               && _queryParameters.SettlementVersion is null;
    }

    private string GetActorsForTotalAmountsSelection(string table, string sql)
    {
        if (_queryParameters.EnergySupplierId is not null)
        {
            sql += $"""
                    AND {table}.{DatabricksContract.GetEnergySupplierIdColumnName()} = '{_queryParameters.EnergySupplierId}'
                    """;
        }

        if (_queryParameters.ChargeOwnerId is not null)
        {
            sql += $"""
                    AND {table}.{DatabricksContract.GetChargeOwnerIdColumnName()} = '{_queryParameters.ChargeOwnerId}'
                    """;
        }
        else
        {
            // When requested for energy supplier, charge owner must be null
            if (_queryParameters is { RequestedForEnergySupplier: true })
            {
                sql += $"""
                        AND {table}.{DatabricksContract.GetChargeOwnerIdColumnName()} is null
                        """;
            }
            else
            {
                if (_queryParameters.RequestedForActorNumber == SystemOperatorActorNumber)
                {
                    sql += $"""
                            AND (
                                {table}.{DatabricksContract.GetChargeOwnerIdColumnName()} = '{SystemOperatorActorNumber}'
                            )
                            """;
                }
                else
                {
                    sql += $"""
                            AND (
                                {table}.{DatabricksContract.GetChargeOwnerIdColumnName()} != '{SystemOperatorActorNumber}'
                                AND {table}.{DatabricksContract.GetChargeOwnerIdColumnName()} is not null
                            )
                            """;
                }
            }
        }

        return sql;
    }

    private string GetActorsForNonTotalAmountsSelection(string table, string sql)
    {
        if (_queryParameters.EnergySupplierId is not null)
        {
            sql += $"""
                    AND {table}.{DatabricksContract.GetEnergySupplierIdColumnName()} = '{_queryParameters.EnergySupplierId}'
                    """;
        }

        if (_queryParameters.ChargeOwnerId is not null)
        {
            if (_queryParameters is { RequestedForActorNumber: SystemOperatorActorNumber })
            {
                sql += $"""
                            AND (
                               {table}.{DatabricksContract.GetChargeOwnerIdColumnName()} = '{SystemOperatorActorNumber}'
                                 AND {table}.{DatabricksContract.GetIsTaxColumnName()} = false
                            )
                        """;
            }
            else if (_queryParameters is { ChargeOwnerId: SystemOperatorActorNumber })
            {
                sql += $"""
                            AND (
                               {table}.{DatabricksContract.GetChargeOwnerIdColumnName()} = '{SystemOperatorActorNumber}'
                                 AND {table}.{DatabricksContract.GetIsTaxColumnName()} = true
                            )
                        """;
            }
            else
            {
                sql += $"""
                            AND (
                               {table}.{DatabricksContract.GetChargeOwnerIdColumnName()} = '{_queryParameters.ChargeOwnerId}' 
                            )
                        """;
            }
        }

        if (_queryParameters is { RequestedForEnergySupplier: false, ChargeOwnerId: null })
        {
            if (_queryParameters.RequestedForActorNumber == SystemOperatorActorNumber)
            {
                sql += $"""
                        AND (
                           {table}.{DatabricksContract.GetChargeOwnerIdColumnName()} = '{SystemOperatorActorNumber}'
                             AND {table}.{DatabricksContract.GetIsTaxColumnName()} = false
                        )
                    """;
            }
            else
            {
                sql += $"""
                         AND (
                            ({table}.{DatabricksContract.GetChargeOwnerIdColumnName()} != '{SystemOperatorActorNumber}' 
                                OR {table}.{DatabricksContract.GetIsTaxColumnName()} = true)
                         )
                     """;
            }
        }

        return sql;
    }

    private string GetChargeTypeSelection(
        string? chargeCode,
        ChargeType? chargeType,
        string table)
    {
        if (chargeCode == null && chargeType == null)
            throw new ArgumentException("Both chargeCode and chargeType cannot be null");

        var sqlStatements = new List<string>();

        if (!string.IsNullOrEmpty(chargeCode))
            sqlStatements.Add($"{table}.{DatabricksContract.GetChargeCodeColumnName()} = '{chargeCode}'");

        if (chargeType != null)
        {
            sqlStatements.Add(
                $"{table}.{DatabricksContract.GetChargeTypeColumnName()} = '{ChargeTypeMapper.ToDeltaTableValue(chargeType)}'");
        }

        var combinedString = string.Join(" AND ", sqlStatements);

        if (sqlStatements.Count > 1)
            combinedString = $"({combinedString})";

        return combinedString;
    }
}
