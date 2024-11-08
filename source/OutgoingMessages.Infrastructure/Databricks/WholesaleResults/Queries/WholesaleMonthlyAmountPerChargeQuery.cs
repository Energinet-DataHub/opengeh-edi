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

using System.Collections.Immutable;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableConstants;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableMappers;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.Factories;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.SqlStatements;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Extensions.Options;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.WholesaleResultMessages;
using Microsoft.Extensions.Logging;
using EventId = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.EventId;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Queries;

public class WholesaleMonthlyAmountPerChargeQuery(
    ILogger logger,
    EdiDatabricksOptions ediDatabricksOptions,
    EventId eventId,
    Guid calculationId,
    string? energySupplier)
    : WholesaleResultQueryBase<WholesaleMonthlyAmountPerChargeMessageDto>(
        logger,
        ediDatabricksOptions,
        calculationId,
        energySupplier)
{
    private readonly EventId _eventId = eventId;
    private readonly ILogger _logger = logger;

    public override string DataObjectName => "monthly_amounts_per_charge_v1";

    public override Dictionary<string, (string DataType, bool IsNullable)> SchemaDefinition => new()
    {
        { WholesaleResultColumnNames.CalculationId,             (DeltaTableCommonTypes.String,              false) },
        { WholesaleResultColumnNames.CalculationType,           (DeltaTableCommonTypes.String,              false) },
        { WholesaleResultColumnNames.CalculationVersion,        (DeltaTableCommonTypes.BigInt,              false) },
        { WholesaleResultColumnNames.ResultId,                  (DeltaTableCommonTypes.String,              false) },
        { WholesaleResultColumnNames.GridAreaCode,              (DeltaTableCommonTypes.String,              false) },
        { WholesaleResultColumnNames.EnergySupplierId,          (DeltaTableCommonTypes.String,              false) },
        { WholesaleResultColumnNames.ChargeCode,                (DeltaTableCommonTypes.String,              false) },
        { WholesaleResultColumnNames.ChargeType,                (DeltaTableCommonTypes.String,              false) },
        { WholesaleResultColumnNames.ChargeOwnerId,             (DeltaTableCommonTypes.String,              false) },
        { WholesaleResultColumnNames.QuantityUnit,              (DeltaTableCommonTypes.String,              false) },
        { WholesaleResultColumnNames.IsTax,                     (DeltaTableCommonTypes.String,              false) },
        { WholesaleResultColumnNames.Currency,                  (DeltaTableCommonTypes.String,              false) },
        { WholesaleResultColumnNames.Time,                      (DeltaTableCommonTypes.Timestamp,           false) },
        { WholesaleResultColumnNames.Amount,                    (DeltaTableCommonTypes.Decimal18x3,         true) },
    };

    protected override string ActorColumnName => WholesaleResultColumnNames.EnergySupplierId;

    protected override Task<WholesaleMonthlyAmountPerChargeMessageDto> CreateWholesaleResultAsync(
        DatabricksSqlRow databricksSqlRow,
        IReadOnlyCollection<WholesaleTimeSeriesPoint> timeSeriesPoints,
        ImmutableDictionary<string, ActorNumber>? gridAreaOwnerDictionary)
    {
        var gridAreaCode = databricksSqlRow.ToNonEmptyString(WholesaleResultColumnNames.GridAreaCode);
        var chargeOwnerId = ActorNumber.Create(databricksSqlRow.ToNonEmptyString(WholesaleResultColumnNames.ChargeOwnerId));
        var calculationResultId = databricksSqlRow.ToGuid(WholesaleResultColumnNames.ResultId);
        var originalChargeOwnerReceiverId = ActorNumber.Create(databricksSqlRow.ToNonEmptyString(WholesaleResultColumnNames.ChargeOwnerId));
        var chargeOwnerReceiverId = originalChargeOwnerReceiverId;
        var isTax = databricksSqlRow.ToBool(WholesaleResultColumnNames.IsTax);

        if (isTax)
        {
            var gridAreaOwner = gridAreaOwnerDictionary![gridAreaCode];

            chargeOwnerReceiverId = gridAreaOwner;
            _logger.LogInformation("Message created from CalculationResultId: {CalculationResultId}, was tax. ChargeOwnerReceiver was changed from {ChargeOwnerReceiverId} to {NewChargeOwnerReceiverId}", calculationResultId, originalChargeOwnerReceiverId, chargeOwnerReceiverId);
        }
        else
        {
            if (chargeOwnerId != DataHubDetails.SystemOperatorActorNumber)
            {
                var gridAreaOwner = gridAreaOwnerDictionary![gridAreaCode];

                chargeOwnerReceiverId = gridAreaOwner;
                _logger.LogInformation(
                    "Message created from CalculationResultId: {CalculationResultId} had old charge owner. ChargeOwnerReceiver was changed from {ChargeOwnerReceiverId} to {NewChargeOwnerReceiverId}",
                    calculationResultId,
                    originalChargeOwnerReceiverId,
                    chargeOwnerReceiverId);
            }
        }

        var (businessReason, settlementVersion) = BusinessReasonAndSettlementVersionMapper.FromDeltaTableValue(
            databricksSqlRow.ToNonEmptyString(WholesaleResultColumnNames.CalculationType));

        var chargeType = ChargeTypeMapper.FromDeltaTableValue(databricksSqlRow.ToNonEmptyString(WholesaleResultColumnNames.ChargeType));
        return Task.FromResult(
            new WholesaleMonthlyAmountPerChargeMessageDto(
            eventId: _eventId,
            calculationId: databricksSqlRow.ToGuid(WholesaleResultColumnNames.CalculationId),
            calculationResultId: calculationResultId,
            calculationResultVersion: databricksSqlRow.ToLong(WholesaleResultColumnNames.CalculationVersion),
            energySupplierReceiverId: ActorNumber.Create(
                databricksSqlRow.ToNonEmptyString(WholesaleResultColumnNames.EnergySupplierId)),
            chargeOwnerReceiverId: chargeOwnerReceiverId,
            chargeOwnerId: chargeOwnerId,
            businessReason: businessReason.Name,
            gridAreaCode: gridAreaCode,
            isTax: isTax,
            period: PeriodFactory.GetPeriod(timeSeriesPoints, Resolution.Monthly),
            quantityUnit: MeasurementUnitMapper.FromDeltaTableValue(databricksSqlRow.ToNullableString(WholesaleResultColumnNames.QuantityUnit)),
            currency: CurrencyMapper.FromDeltaTableValue(databricksSqlRow.ToNonEmptyString(WholesaleResultColumnNames.Currency)),
            chargeType: chargeType,
            settlementVersion: settlementVersion,
            chargeCode: databricksSqlRow.ToNullableString(WholesaleResultColumnNames.ChargeCode),
            points: timeSeriesPoints
                .Select(
                    (p, index) => new WholesaleServicesPoint(
                        index + 1, // Position starts at 1, so position = index + 1
                        p.Quantity,
                        p.Price,
                        p.Amount,
                        // Quantity quality is not relevant for monthly amounts
                        null))
                .ToList()
                .AsReadOnly()));
    }
}
