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
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableConstants;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableMappers;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.Factories;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.SqlStatements;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Extensions.Options;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Queries;

public class WholesaleAmountPerChargeQuery(
    EdiDatabricksOptions ediDatabricksOptions,
    IMasterDataClient masterDataClient,
    EventId eventId,
    Guid calculationId)
    : WholesaleResultQueryBase<WholesaleAmountPerChargeMessageDto>(
        ediDatabricksOptions,
        calculationId)
{
    private readonly IMasterDataClient _masterDataClient = masterDataClient;
    private readonly EventId _eventId = eventId;

    public override string DataObjectName => "amounts_per_charge_v1";

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
        { WholesaleResultColumnNames.Resolution,                (DeltaTableCommonTypes.String,              false) },
        { WholesaleResultColumnNames.QuantityUnit,              (DeltaTableCommonTypes.String,              false) },
        { WholesaleResultColumnNames.MeteringPointType,         (DeltaTableCommonTypes.String,              false) },
        { WholesaleResultColumnNames.SettlementMethod,          (DeltaTableCommonTypes.String,              true) },
        { WholesaleResultColumnNames.IsTax,                     (DeltaTableCommonTypes.String,              false) },
        { WholesaleResultColumnNames.Currency,                  (DeltaTableCommonTypes.String,              false) },
        { WholesaleResultColumnNames.Time,                      (DeltaTableCommonTypes.Timestamp,           false) },
        { WholesaleResultColumnNames.Quantity,                  (DeltaTableCommonTypes.Decimal18x3,         false) },
        { WholesaleResultColumnNames.QuantityQualities,         (DeltaTableCommonTypes.ArrayOfStrings,      true) },
        { WholesaleResultColumnNames.Price,                     (DeltaTableCommonTypes.Decimal18x3,         true) },
        { WholesaleResultColumnNames.Amount,                    (DeltaTableCommonTypes.Decimal18x3,         true) },
    };

    protected override async Task<WholesaleAmountPerChargeMessageDto> CreateWholesaleResultAsync(
        DatabricksSqlRow databricksSqlRow,
        IReadOnlyCollection<WholesaleTimeSeriesPoint> timeSeriesPoints)
    {
        var gridAreaCode = databricksSqlRow.ToNonEmptyString(WholesaleResultColumnNames.GridAreaCode);
        var chargeOwnerId = ActorNumber.Create(databricksSqlRow.ToNonEmptyString(WholesaleResultColumnNames.ChargeOwnerId));
        var isTax = databricksSqlRow.ToBool(WholesaleResultColumnNames.IsTax);

        var chargeOwnerReceiverId = await GetChargeOwnerReceiverAsync(
            gridAreaCode,
            chargeOwnerId,
            isTax).ConfigureAwait(false);
        var (businessReason, settlementVersion) = BusinessReasonAndSettlementVersionMapper.FromDeltaTableValue(
            databricksSqlRow.ToNonEmptyString(WholesaleResultColumnNames.CalculationType));
        var resolution =
            ResolutionMapper.FromDeltaTableValue(
                databricksSqlRow.ToNonEmptyString(WholesaleResultColumnNames.Resolution));

        var chargeType = ChargeTypeMapper.FromDeltaTableValue(databricksSqlRow.ToNullableString(WholesaleResultColumnNames.ChargeType));
        return new WholesaleAmountPerChargeMessageDto(
            eventId: _eventId,
            calculationId: databricksSqlRow.ToGuid(WholesaleResultColumnNames.CalculationId),
            calculationResultId: databricksSqlRow.ToGuid(WholesaleResultColumnNames.ResultId),
            calculationResultVersion: databricksSqlRow.ToLong(WholesaleResultColumnNames.CalculationVersion),
            energySupplierReceiverId: ActorNumber.Create(
                databricksSqlRow.ToNonEmptyString(WholesaleResultColumnNames.EnergySupplierId)),
            chargeOwnerReceiverId: chargeOwnerReceiverId,
            chargeOwnerId: chargeOwnerId,
            businessReason: businessReason.Name,
            gridAreaCode: gridAreaCode,
            isTax: isTax,
            period: PeriodFactory.GetPeriod(timeSeriesPoints, resolution),
            quantityUnit: MeasurementUnitMapper.FromDeltaTableValue(databricksSqlRow.ToNullableString(WholesaleResultColumnNames.QuantityUnit)),
            currency: CurrencyMapper.FromDeltaTableValue(databricksSqlRow.ToNonEmptyString(WholesaleResultColumnNames.Currency)),
            chargeType: chargeType,
            resolution: resolution,
            settlementVersion: settlementVersion,
            meteringPointType: MeteringPointTypeMapper.FromDeltaTableValue(databricksSqlRow.ToNonEmptyString(WholesaleResultColumnNames.MeteringPointType)),
            settlementMethod: SettlementMethodMapper.FromDeltaTableValue(databricksSqlRow.ToNullableString(WholesaleResultColumnNames.SettlementMethod)),
            chargeCode: databricksSqlRow.ToNullableString(WholesaleResultColumnNames.ChargeCode),
            points: timeSeriesPoints
                .Select(
                    (p, index) => new WholesaleServicesPoint(
                        index + 1, // Position starts at 1, so position = index + 1
                        p.Quantity,
                        p.Price,
                        p.Amount,
                        GetQuantityQuality(p.Price, p.Qualities, chargeType)))
                .ToList()
                .AsReadOnly());
    }

    /// <summary>
    /// Quantity quality mappings is defined by the business.
    /// See "https://energinet.atlassian.net/wiki/spaces/D3/pages/529989633/QuantityQuality" for more information.
    /// </summary>
    private static CalculatedQuantityQuality GetQuantityQuality(decimal? price, IReadOnlyCollection<QuantityQuality> qualities, ChargeType? chargeType)
    {
        if (price == null)
        {
            return CalculatedQuantityQuality.Missing;
        }

        return chargeType == ChargeType.Subscription || chargeType == ChargeType.Fee
            ? CalculatedQuantityQuality.Calculated
            : MapQuantityQualitiesToQuality(qualities);
    }

    private static CalculatedQuantityQuality MapQuantityQualitiesToQuality(
        IReadOnlyCollection<QuantityQuality> qualities)
    {
        ArgumentNullException.ThrowIfNull(qualities);

        return (missing: qualities.Contains(QuantityQuality.Missing),
                estimated: qualities.Contains(QuantityQuality.Estimated),
                measured: qualities.Contains(QuantityQuality.Measured),
                calculated: qualities.Contains(QuantityQuality.Calculated)) switch
        {
            (missing: true, estimated: false, measured: false, calculated: false) => CalculatedQuantityQuality.Missing,
            (missing: true, _, _, _) => CalculatedQuantityQuality.Incomplete,
            (_, estimated: true, _, _) => CalculatedQuantityQuality.Calculated,
            (_, _, measured: true, _) => CalculatedQuantityQuality.Calculated,
            (_, _, _, calculated: true) => CalculatedQuantityQuality.Calculated,
            _ => CalculatedQuantityQuality.NotAvailable,
        };
    }

    private async Task<ActorNumber> GetChargeOwnerReceiverAsync(string gridAreaCode, ActorNumber chargeOwnerId, bool isTax)
    {
        return isTax
            ? await _masterDataClient
                .GetGridOwnerForGridAreaCodeAsync(gridAreaCode, CancellationToken.None)
                .ConfigureAwait(false)
            : chargeOwnerId;
    }
}
