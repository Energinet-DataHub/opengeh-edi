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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults.Mappers.EnergyResults;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableMappers;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults.EnergyResults;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults.Statements;

public sealed class AggregatedTimeSeriesQuerySnippetProvider(
    AggregatedTimeSeriesQueryParameters queryParameters,
    IAggregatedTimeSeriesDatabricksContract databricksContract)
{
    private readonly AggregatedTimeSeriesQueryParameters _queryParameters = queryParameters;

    internal IAggregatedTimeSeriesDatabricksContract DatabricksContract { get; } = databricksContract;

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

    internal string GetSelection(string table)
    {
        return $"""
                ({GetTimeSeriesTypeSelection(
                    _queryParameters,
                    table)})
                """;
    }

    internal string GetLatestOrFixedCalculationTypeSelection(
        string prefix,
        IReadOnlyCollection<CalculationTypeForGridArea> calculationTypeForGridAreas)
    {
        if (!IsQueryForLatestCorrection())
        {
            return $"""
                    {prefix}.{DatabricksContract.GetCalculationTypeColumnName()} = '{CalculationTypeMapper.ToDeltaTableValue(_queryParameters.BusinessReason, _queryParameters.SettlementVersion)}'
                    """;
        }

        if (calculationTypeForGridAreas.Count <= 0)
        {
            return """
                   FALSE
                   """;
        }

        var calculationTypePerGridAreaConstraints = calculationTypeForGridAreas
            .Select(ctpga => $"""
                              ({prefix}.{DatabricksContract.GetGridAreaCodeColumnName()} = '{ctpga.GridArea}' AND {prefix}.{DatabricksContract.GetCalculationTypeColumnName()} = '{ctpga.CalculationType}')
                              """);

        return $"({string.Join(" OR ", calculationTypePerGridAreaConstraints)})";
    }

    private bool IsQueryForLatestCorrection()
    {
        return _queryParameters.BusinessReason == BusinessReason.Correction
               && _queryParameters.SettlementVersion is null;
    }

    private string GetTimeSeriesTypeSelection(
        AggregatedTimeSeriesQueryParameters parameters,
        string table)
    {
        var meteringPointTypeDeltaTableValue = MeteringPointTypeMapper.ToDeltaTableValue(parameters.MeteringPointType);
        var settlementMethodDeltaTableValue = SettlementMethodMapper.ToDeltaTableValue(parameters.SettlementMethod);

        var sqlConstraint =
                $"""
                     {(meteringPointTypeDeltaTableValue is not null ? $"{table}.{DatabricksContract.GetMeteringPointTypeColumnName()} = '{meteringPointTypeDeltaTableValue}' AND " : string.Empty)}
                     {(meteringPointTypeDeltaTableValue is not null ? $"{table}.{DatabricksContract.GetSettlementMethodColumnName()} = '{settlementMethodDeltaTableValue}' AND " : string.Empty)} 
                     ({table}.{DatabricksContract.GetTimeColumnName()} >= '{parameters.Period.Start}'
                          AND {table}.{DatabricksContract.GetTimeColumnName()} < '{parameters.Period.End}')
                     """.Trim();

        if (parameters.GridAreaCodes.Count > 0)
        {
            sqlConstraint +=
                $" AND {table}.{DatabricksContract.GetGridAreaCodeColumnName()} IN ({string.Join(",", parameters.GridAreaCodes.Select(gridAreaCode => $"'{gridAreaCode}'"))})";
        }

        if (parameters.EnergySupplierId is not null)
        {
            sqlConstraint +=
                $" AND {table}.{DatabricksContract.GetEnergySupplierIdColumnName()} = '{parameters.EnergySupplierId}'";
        }

        if (parameters.BalanceResponsibleId is not null)
        {
            sqlConstraint +=
                $" AND {table}.{DatabricksContract.GetBalanceResponsiblePartyIdColumnName()} = '{parameters.BalanceResponsibleId}'";
        }

        return sqlConstraint;
    }
}
