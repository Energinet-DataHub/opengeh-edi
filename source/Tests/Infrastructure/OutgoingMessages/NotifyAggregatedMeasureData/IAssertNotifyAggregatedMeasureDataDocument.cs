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
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.Asserts;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;
using Resolution = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Resolution;
using SettlementVersion = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.SettlementVersion;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.NotifyAggregatedMeasureData;

/// <summary>
/// Assertion helper for aggregation result documents
/// </summary>
public interface IAssertNotifyAggregatedMeasureDataDocument
{
    /// <summary>
    /// Asserts message id
    /// </summary>
    /// <param name="expectedMessageId"></param>
    IAssertNotifyAggregatedMeasureDataDocument HasMessageId(string expectedMessageId);

    /// <summary>
    /// Assert message id exists
    /// </summary>
    IAssertNotifyAggregatedMeasureDataDocument MessageIdExists();

    /// <summary>
    /// Assert sender id
    /// </summary>
    /// <param name="expectedSenderId"></param>
    IAssertNotifyAggregatedMeasureDataDocument HasSenderId(string expectedSenderId);

    /// <summary>
    /// Asserts receiver id
    /// </summary>
    /// <param name="expectedReceiverId"></param>
    IAssertNotifyAggregatedMeasureDataDocument HasReceiverId(string expectedReceiverId);

    /// <summary>
    /// Asserts time stamp
    /// </summary>
    /// <param name="expectedTimestamp"></param>
    IAssertNotifyAggregatedMeasureDataDocument HasTimestamp(string expectedTimestamp);

    /// <summary>
    /// Asserts transaction id
    /// </summary>
    /// <param name="expectedTransactionId"></param>
    IAssertNotifyAggregatedMeasureDataDocument HasTransactionId(TransactionId expectedTransactionId);

    /// <summary>
    /// Assert transaction id exists
    /// </summary>
    IAssertNotifyAggregatedMeasureDataDocument TransactionIdExists();

    /// <summary>
    /// Asserts grid area code
    /// </summary>
    /// <param name="expectedGridAreaCode"></param>
    IAssertNotifyAggregatedMeasureDataDocument HasGridAreaCode(string expectedGridAreaCode);

    /// <summary>
    /// Asserts balance supplier number
    /// </summary>
    /// <param name="expectedBalanceResponsibleNumber"></param>
    IAssertNotifyAggregatedMeasureDataDocument HasBalanceResponsibleNumber(string expectedBalanceResponsibleNumber);

    /// <summary>
    /// Asserts energy supplier number
    /// </summary>
    /// <param name="expectedEnergySupplierNumber"></param>
    IAssertNotifyAggregatedMeasureDataDocument HasEnergySupplierNumber(string expectedEnergySupplierNumber);

    /// <summary>
    /// Asserts product code
    /// </summary>
    /// <param name="expectedProductCode"></param>
    IAssertNotifyAggregatedMeasureDataDocument HasProductCode(string expectedProductCode);

    /// <summary>
    /// Asserts period
    /// </summary>
    /// <param name="expectedPeriod"></param>
    IAssertNotifyAggregatedMeasureDataDocument HasPeriod(Period expectedPeriod);

    /// <summary>
    /// Asserts a point
    /// </summary>
    /// <param name="position"></param>
    /// <param name="quantity"></param>
    IAssertNotifyAggregatedMeasureDataDocument HasPoint(int position, int quantity);

    /// <summary>
    /// Asserts document validity
    /// </summary>
    Task<IAssertNotifyAggregatedMeasureDataDocument> DocumentIsValidAsync();

    /// <summary>
    /// Asserts the settlement method is not present
    /// </summary>
    IAssertNotifyAggregatedMeasureDataDocument SettlementMethodIsNotPresent();

    /// <summary>
    /// Asserts the settlement version is not present
    /// </summary>
    IAssertNotifyAggregatedMeasureDataDocument SettlementVersionIsNotPresent();

    /// <summary>
    /// Asserts the energy supplier number is not present
    /// </summary>
    IAssertNotifyAggregatedMeasureDataDocument EnergySupplierNumberIsNotPresent();

    /// <summary>
    /// Asserts the balance responsible number is not present
    /// </summary>
    IAssertNotifyAggregatedMeasureDataDocument BalanceResponsibleNumberIsNotPresent();

    /// <summary>
    /// Asserts the quantity is not present
    /// </summary>
    /// <param name="position"></param>
    IAssertNotifyAggregatedMeasureDataDocument QuantityIsNotPresentForPosition(int position);

    /// <summary>
    /// Asserts the quality is not present
    /// </summary>
    /// <param name="position"></param>
    IAssertNotifyAggregatedMeasureDataDocument QualityIsNotPresentForPosition(int position);

    /// <summary>
    /// Asserts the process type.
    /// </summary>
    /// <param name="businessReason"></param>
    IAssertNotifyAggregatedMeasureDataDocument HasBusinessReason(BusinessReason businessReason);

    /// <summary>
    /// Asserts the SettlementVersion
    /// </summary>
    /// <param name="settlementVersion"></param>
    IAssertNotifyAggregatedMeasureDataDocument HasSettlementVersion(SettlementVersion settlementVersion);

    /// <summary>
    /// Asserts the OriginalTransactionIdReference
    /// </summary>
    /// <param name="originalTransactionIdReference"></param>
    IAssertNotifyAggregatedMeasureDataDocument HasOriginalTransactionIdReference(
        TransactionId originalTransactionIdReference);

    /// <summary>
    /// Asserts the OriginalTransactionIdReference does not exist
    /// </summary>
    IAssertNotifyAggregatedMeasureDataDocument OriginalTransactionIdReferenceDoesNotExist();

    /// <summary>
    /// Asserts the settlement method
    /// </summary>
    IAssertNotifyAggregatedMeasureDataDocument HasSettlementMethod(SettlementMethod settlementMethod);

    /// <summary>
    ///     Asserts the quality is present with the given code
    /// </summary>
    IAssertNotifyAggregatedMeasureDataDocument QualityIsPresentForPosition(int position, string quantityQualityCode);

    /// <summary>
    ///     Asserts the calculation result version is present with the given number
    /// </summary>
    IAssertNotifyAggregatedMeasureDataDocument HasCalculationResultVersion(long version);

    /// <summary>
    /// Asserts the metering point type
    /// </summary>
    IAssertNotifyAggregatedMeasureDataDocument HasMeteringPointType(MeteringPointType meteringPointType);

    /// <summary>
    /// Asserts the quantity measurement unit
    /// </summary>
    IAssertNotifyAggregatedMeasureDataDocument HasQuantityMeasurementUnit(MeasurementUnit quantityMeasurementUnit);

    /// <summary>
    /// Asserts the resolution
    /// </summary>
    IAssertNotifyAggregatedMeasureDataDocument HasResolution(Resolution resolution);

    // /// <summary>
    // /// Asserts the points based on the Wholesale response
    // /// </summary>
    // IAssertNotifyAggregatedMeasureDataDocument HasPoints(IReadOnlyCollection<TimeSeriesPoint> points);

    /// <summary>
    /// Asserts the points
    /// </summary>
    IAssertNotifyAggregatedMeasureDataDocument HasPoints(IReadOnlyCollection<TimeSeriesPointAssertionInput> points);
}
