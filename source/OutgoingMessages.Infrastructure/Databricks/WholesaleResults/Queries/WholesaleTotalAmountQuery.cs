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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableConstants;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableMappers;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.Factories;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.SqlStatements;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Extensions.Options;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Microsoft.Extensions.Logging;
using EventId = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.EventId;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Queries;

public class WholesaleTotalAmountQuery(
    ILogger logger,
    EdiDatabricksOptions ediDatabricksOptions,
    EventId eventId,
    Guid calculationId,
    string? energySupplier)
    : WholesaleResultQueryBase<WholesaleTotalAmountMessageDto>(
        logger,
        ediDatabricksOptions,
        calculationId,
        energySupplier)
{
    private readonly EventId _eventId = eventId;

    public override string DataObjectName => "total_monthly_amounts_v1";

    public override string ActorColumnName => WholesaleResultColumnNames.EnergySupplierId;

    public override Dictionary<string, (string DataType, bool IsNullable)> SchemaDefinition => new()
    {
        { WholesaleResultColumnNames.CalculationId, (DeltaTableCommonTypes.String, false) },
        { WholesaleResultColumnNames.CalculationType, (DeltaTableCommonTypes.String, false) },
        { WholesaleResultColumnNames.CalculationVersion, (DeltaTableCommonTypes.BigInt, false) },
        { WholesaleResultColumnNames.ResultId, (DeltaTableCommonTypes.String, false) },
        { WholesaleResultColumnNames.GridAreaCode, (DeltaTableCommonTypes.String, false) },
        { WholesaleResultColumnNames.EnergySupplierId, (DeltaTableCommonTypes.String, false) },
        { WholesaleResultColumnNames.ChargeOwnerId, (DeltaTableCommonTypes.String, true) },
        { WholesaleResultColumnNames.Currency, (DeltaTableCommonTypes.String, false) },
        { WholesaleResultColumnNames.Time, (DeltaTableCommonTypes.Timestamp, false) },
        { WholesaleResultColumnNames.Amount, (DeltaTableCommonTypes.Decimal18x3, true) },
    };

    protected override Task<WholesaleTotalAmountMessageDto> CreateWholesaleResultAsync(
        DatabricksSqlRow databricksSqlRow,
        IReadOnlyCollection<WholesaleTimeSeriesPoint> timeSeriesPoints)
    {
        var receiver = GetReceiver(databricksSqlRow);
        var (businessReason, settlementVersion) = BusinessReasonAndSettlementVersionMapper.FromDeltaTableValue(
            databricksSqlRow.ToNonEmptyString(WholesaleResultColumnNames.CalculationType));

        return Task.FromResult(new WholesaleTotalAmountMessageDto(
            eventId: _eventId,
            calculationId: databricksSqlRow.ToGuid(WholesaleResultColumnNames.CalculationId),
            calculationResultId: databricksSqlRow.ToGuid(WholesaleResultColumnNames.ResultId),
            calculationResultVersion: databricksSqlRow.ToLong(WholesaleResultColumnNames.CalculationVersion),
            receiverNumber: receiver.ActorNumber,
            receiverRole: receiver.ActorRole,
            energySupplierId: ActorNumber.Create(
                databricksSqlRow.ToNonEmptyString(WholesaleResultColumnNames.EnergySupplierId)),
            businessReason: businessReason.Name,
            gridAreaCode: databricksSqlRow.ToNonEmptyString(WholesaleResultColumnNames.GridAreaCode),
            period: PeriodFactory.GetPeriod(timeSeriesPoints, Resolution.Monthly),
            currency: CurrencyMapper.FromDeltaTableValue(
                databricksSqlRow.ToNonEmptyString(WholesaleResultColumnNames.Currency)),
            settlementVersion: settlementVersion,
            points: timeSeriesPoints
                .Select(
                    (p, index) => new WholesaleServicesPoint(
                        index + 1, // Position starts at 1, so position = index + 1
                        p.Quantity,
                        p.Price,
                        p.Amount,
                        p.Amount != null ? CalculatedQuantityQuality.Calculated : CalculatedQuantityQuality.Missing))
                .ToList()
                .AsReadOnly()));
    }

    private static ActorNumber? GetChargeOwnerNumber(DatabricksSqlRow databricksSqlRow)
    {
        var databricksChargeOwnerId = databricksSqlRow.ToNullableString(WholesaleResultColumnNames.ChargeOwnerId);
        var chargeOwnerNumber =
            databricksChargeOwnerId is not null ? ActorNumber.Create(databricksChargeOwnerId) : null;
        return chargeOwnerNumber;
    }

    private static (ActorNumber ActorNumber, ActorRole ActorRole) GetReceiver(
        DatabricksSqlRow databricksSqlRow)
    {
        var chargeOwnerNumber = GetChargeOwnerNumber(databricksSqlRow);
        var energySupplierNumber =
            ActorNumber.Create(databricksSqlRow.ToNonEmptyString(WholesaleResultColumnNames.EnergySupplierId));
        if (chargeOwnerNumber is not null)
        {
            //ChargeOwner can either be grid operator or system operator.
            return (
                chargeOwnerNumber,
                GetChargeOwnerRole(chargeOwnerNumber));
        }

        return (energySupplierNumber, ActorRole.EnergySupplier);
    }

    private static ActorRole GetChargeOwnerRole(ActorNumber chargeOwnerId)
    {
        return chargeOwnerId == DataHubDetails.SystemOperatorActorNumber
            ? ActorRole.SystemOperator
            : ActorRole.GridOperator;
    }
}
