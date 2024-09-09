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
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.WholesaleResultMessages;
using Energinet.DataHub.Edi.Responses;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;
using Resolution = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Resolution;
using SettlementVersion = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.SettlementVersion;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.NotifyWholesaleServices;

/// <summary>
/// Assertion helper for aggregation result documents
/// </summary>
public interface IAssertNotifyWholesaleServicesDocument
{
    /// <summary>
    /// Asserts document validity
    /// </summary>
    Task<IAssertNotifyWholesaleServicesDocument> DocumentIsValidAsync();

    #region header validation

    /// <summary>
    /// Asserts message id in header
    /// </summary>
    /// <param name="expectedMessageId"></param>
    IAssertNotifyWholesaleServicesDocument HasMessageId(string expectedMessageId);

    /// <summary>
    /// Asserts message id exists
    /// </summary>
    IAssertNotifyWholesaleServicesDocument MessageIdExists();

    /// <summary>
    /// Asserts the process type in header
    /// </summary>
    /// <param name="expectedBusinessReason"></param>
    /// <param name="codeListType"></param>
    IAssertNotifyWholesaleServicesDocument HasBusinessReason(BusinessReason expectedBusinessReason, CodeListType codeListType);

    /// <summary>
    /// Assert sender id in header
    /// </summary>
    IAssertNotifyWholesaleServicesDocument HasSenderId(ActorNumber expectedSenderId, string codingScheme);

    /// <summary>
    /// Assert sender role in header
    /// </summary>
    /// <param name="expectedSenderRole"></param>
    IAssertNotifyWholesaleServicesDocument HasSenderRole(ActorRole expectedSenderRole);

    /// <summary>
    /// Asserts receiver id in header
    /// </summary>
    /// <param name="expectedReceiverId"></param>
    IAssertNotifyWholesaleServicesDocument HasReceiverId(ActorNumber expectedReceiverId);

    /// <summary>
    /// Assert sender role in header
    /// </summary>
    /// <param name="expectedReceiverRole"></param>
    /// <param name="codeListType"></param>
    IAssertNotifyWholesaleServicesDocument HasReceiverRole(ActorRole expectedReceiverRole, CodeListType codeListType);

    /// <summary>
    /// Asserts time stamp in header
    /// </summary>
    /// <param name="expectedTimestamp"></param>
    IAssertNotifyWholesaleServicesDocument HasTimestamp(string expectedTimestamp);

    #endregion

    #region series validation

    /// <summary>
    /// Asserts transaction id of the first series element
    /// </summary>
    /// <param name="expectedTransactionId"></param>
    IAssertNotifyWholesaleServicesDocument HasTransactionId(TransactionId expectedTransactionId);

    /// <summary>
    /// Asserts transaction exists in the first series element
    /// </summary>
    IAssertNotifyWholesaleServicesDocument TransactionIdExists();

    /// <summary>
    /// Asserts the version of the first series element
    /// </summary>
    /// <param name="expectedVersion"></param>
    IAssertNotifyWholesaleServicesDocument HasCalculationVersion(long expectedVersion);

    /// <summary>
    /// Asserts the settlement version of the first series element
    /// </summary>
    /// <param name="expectedSettlementVersion"></param>
    IAssertNotifyWholesaleServicesDocument HasSettlementVersion(SettlementVersion expectedSettlementVersion);

    /// <summary>
    /// Asserts the settlement version is not present
    /// </summary>
    IAssertNotifyWholesaleServicesDocument SettlementVersionDoesNotExist();

    /// <summary>
    /// Asserts the reference id of the first series element
    /// </summary>
    /// <param name="expectedOriginalTransactionIdReference"></param>
    IAssertNotifyWholesaleServicesDocument HasOriginalTransactionIdReference(
        TransactionId expectedOriginalTransactionIdReference);

    /// <summary>
    /// Asserts that the first series element does not have a reference id
    /// </summary>
    IAssertNotifyWholesaleServicesDocument OriginalTransactionIdReferenceDoesNotExist();

    /// <summary>
    /// Asserts the settlement method of the first series element
    /// </summary>
    /// <param name="expectedSettlementMethod"></param>
    IAssertNotifyWholesaleServicesDocument HasSettlementMethod(SettlementMethod expectedSettlementMethod);

    /// <summary>
    /// Asserts the settlement method is not present
    /// </summary>
    IAssertNotifyWholesaleServicesDocument SettlementMethodDoesNotExist();

    /// <summary>
    /// Asserts the amount sum of the points of the first series element
    /// </summary>
    /// <param name="position"></param>
    /// <param name="expectedPrice"></param>
    IAssertNotifyWholesaleServicesDocument HasPriceForPosition(int position, string? expectedPrice);

    /// <summary>
    /// Asserts the metering point type of the first series element
    /// </summary>
    /// <param name="expectedMeteringPointType"></param>
    IAssertNotifyWholesaleServicesDocument HasMeteringPointType(MeteringPointType expectedMeteringPointType);

    /// <summary>
    /// Asserts the metering point type of the first series element is not present
    /// </summary>
    IAssertNotifyWholesaleServicesDocument MeteringPointTypeDoesNotExist();

    /// <summary>
    /// Asserts the charge type number of the first series element
    /// </summary>
    /// <param name="expectedChargeTypeNumber"></param>
    IAssertNotifyWholesaleServicesDocument HasChargeCode(string expectedChargeTypeNumber);

    /// <summary>
    /// Asserts the charge type number of the first series element is not present
    /// </summary>
    IAssertNotifyWholesaleServicesDocument ChargeCodeDoesNotExist();

    /// <summary>
    /// Asserts the charge type of the first series element
    /// </summary>
    /// <param name="expectedChargeType"></param>
    IAssertNotifyWholesaleServicesDocument HasChargeType(ChargeType expectedChargeType);

    /// <summary>
    /// Asserts the charge type of the first series element is not present
    /// </summary>
    IAssertNotifyWholesaleServicesDocument ChargeTypeDoesNotExist();

    /// <summary>
    /// Asserts the charge type owner of the first series element
    /// </summary>
    IAssertNotifyWholesaleServicesDocument HasChargeTypeOwner(
        ActorNumber expectedChargeTypeOwner,
        string codingScheme);

    /// <summary>
    /// Asserts the charge type owner of the first series element is not present
    /// </summary>
    IAssertNotifyWholesaleServicesDocument ChargeTypeOwnerDoesNotExist();

    /// <summary>
    /// Asserts grid area code of the first series element
    /// </summary>
    IAssertNotifyWholesaleServicesDocument HasGridAreaCode(string expectedGridAreaCode, string codingScheme);

    /// <summary>
    /// Asserts energy supplier number of the first series element
    /// </summary>
    IAssertNotifyWholesaleServicesDocument HasEnergySupplierNumber(
        ActorNumber expectedEnergySupplierNumber,
        string codingScheme);

    /// <summary>
    /// Assets the product of the first series element
    /// </summary>
    IAssertNotifyWholesaleServicesDocument HasProductCode(string expectedProductCode);

    /// <summary>
    /// Asserts the quantity measure unit of the first series element
    /// </summary>
    /// <param name="expectedMeasurementUnit"></param>
    IAssertNotifyWholesaleServicesDocument HasQuantityMeasurementUnit(MeasurementUnit expectedMeasurementUnit);

    /// <summary>
    /// Asserts the price measure unit of the first series element is not present
    /// </summary>
    IAssertNotifyWholesaleServicesDocument PriceMeasurementUnitDoesNotExist();

    /// <summary>
    /// Asserts the price measure unit of the first series element
    /// </summary>
    /// <param name="expectedPriceMeasurementUnit"></param>
    IAssertNotifyWholesaleServicesDocument HasPriceMeasurementUnit(MeasurementUnit expectedPriceMeasurementUnit);

    /// <summary>
    /// Asserts the currency of the first series element
    /// </summary>
    /// <param name="expectedPriceUnit"></param>
    IAssertNotifyWholesaleServicesDocument HasCurrency(Currency expectedPriceUnit);

    /// <summary>
    /// Asserts period of the first series element
    /// </summary>
    /// <param name="expectedPeriod"></param>
    IAssertNotifyWholesaleServicesDocument HasPeriod(Period expectedPeriod);

    /// <summary>
    /// Asserts the resulution of the first series element
    /// </summary>
    /// <param name="resolution"></param>
    IAssertNotifyWholesaleServicesDocument HasResolution(Resolution resolution);

    /// <summary>
    /// Asserts the resulution of the first series element is not present
    /// </summary>
    IAssertNotifyWholesaleServicesDocument ResolutionDoesNotExist();

    /// <summary>
    /// Asserts a point of the first series element
    /// </summary>
    /// <param name="expectedPosition"></param>
    /// <param name="expectedSumQuantity"></param>
    IAssertNotifyWholesaleServicesDocument HasSumQuantityForPosition(int expectedPosition, int expectedSumQuantity);

    /// <summary>
    /// Asserts a point of the first series element
    /// </summary>
    /// <param name="expectedPosition"></param>
    /// <param name="expectedQuantity"></param>
    IAssertNotifyWholesaleServicesDocument HasQuantityForPosition(int expectedPosition, int expectedQuantity);

    /// <summary>
    /// Asserts the quality is present with the given code
    /// </summary>
    IAssertNotifyWholesaleServicesDocument HasQualityForPosition(
        int expectedPosition,
        CalculatedQuantityQuality expectedQuantityQuality);

    /// <summary>
    /// Asserts any points are present in the first series element
    /// </summary>
    IAssertNotifyWholesaleServicesDocument HasAnyPoints();

    /// <summary>
    /// Asserts the list of points exists exactly as given in the first series element
    /// </summary>
    IAssertNotifyWholesaleServicesDocument HasPoints(
        IReadOnlyCollection<WholesaleServicesRequestSeries.Types.Point> points,
        Resolution resolution);

    /// <summary>
    /// Asserts the list of points exists exactly as given in the first series element
    /// </summary>
    IAssertNotifyWholesaleServicesDocument HasPoints(IReadOnlyCollection<WholesaleServicesPoint> points);

    #endregion

    /// <summary>
    /// Asserts the list of points only contains a single point with the given amount and calculated quality
    /// </summary>
    IAssertNotifyWholesaleServicesDocument HasSinglePointWithAmountAndQuality(
        DecimalValue expectedAmount,
        QuantityQuality? expectedQuantityQuality);
}
