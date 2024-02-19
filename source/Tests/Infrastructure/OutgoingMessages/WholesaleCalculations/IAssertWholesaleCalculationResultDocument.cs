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

using System;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.WholesaleCalculations;

/// <summary>
/// Assertion helper for aggregation result documents
/// </summary>
public interface IAssertWholesaleCalculationResultDocument
{
    /// <summary>
    /// Asserts document validity
    /// </summary>
    Task<IAssertWholesaleCalculationResultDocument> DocumentIsValidAsync();

    #region header validation

    /// <summary>
    /// Asserts message id in header
    /// </summary>
    /// <param name="expectedMessageId"></param>
    IAssertWholesaleCalculationResultDocument HasMessageId(string expectedMessageId);

    /// <summary>
    /// Asserts the process type in header
    /// </summary>
    /// <param name="expectedBusinessReason"></param>
    IAssertWholesaleCalculationResultDocument HasBusinessReason(BusinessReason expectedBusinessReason);

    /// <summary>
    /// Assert sender id in header
    /// </summary>
    /// <param name="expectedSenderId"></param>
    IAssertWholesaleCalculationResultDocument HasSenderId(ActorNumber expectedSenderId);

    /// <summary>
    /// Assert sender role in header
    /// </summary>
    /// <param name="expectedSenderRole"></param>
    IAssertWholesaleCalculationResultDocument HasSenderRole(ActorRole expectedSenderRole);

    /// <summary>
    /// Asserts receiver id in header
    /// </summary>
    /// <param name="expectedReceiverId"></param>
    IAssertWholesaleCalculationResultDocument HasReceiverId(ActorNumber expectedReceiverId);

    /// <summary>
    /// Assert sender role in header
    /// </summary>
    /// <param name="expectedReceiverRole"></param>
    IAssertWholesaleCalculationResultDocument HasReceiverRole(ActorRole expectedReceiverRole);

    /// <summary>
    /// Asserts time stamp in header
    /// </summary>
    /// <param name="expectedTimestamp"></param>
    IAssertWholesaleCalculationResultDocument HasTimestamp(string expectedTimestamp);
    #endregion

    #region series validation

    /// <summary>
    /// Asserts transaction id of the first series element
    /// </summary>
    /// <param name="expectedTransactionId"></param>
    IAssertWholesaleCalculationResultDocument HasTransactionId(Guid expectedTransactionId);

    /// <summary>
    /// Asserts the version of the first series element
    /// </summary>
    /// <param name="expectedVersion"></param>
    IAssertWholesaleCalculationResultDocument HasCalculationVersion(int expectedVersion);

    /// <summary>
    /// Asserts the settlement version of the first series element
    /// </summary>
    /// <param name="expectedSettlementVersion"></param>
    IAssertWholesaleCalculationResultDocument HasSettlementVersion(SettlementVersion expectedSettlementVersion);

    /// <summary>
    /// Asserts the reference id of the first series element
    /// </summary>
    /// <param name="expectedOriginalTransactionIdReference"></param>
    IAssertWholesaleCalculationResultDocument HasOriginalTransactionIdReference(string expectedOriginalTransactionIdReference);

    /// <summary>
    /// Asserts the evaluation type of the first series element
    /// </summary>
    /// <param name="expectedMarketEvaluationType"></param>
    IAssertWholesaleCalculationResultDocument HasEvaluationType(string expectedMarketEvaluationType);

    /// <summary>
    /// Asserts the settlement method of the first series element
    /// </summary>
    /// <param name="expectedSettlementMethod"></param>
    IAssertWholesaleCalculationResultDocument HasSettlementMethod(SettlementType expectedSettlementMethod);

    /// <summary>
    /// Asserts the charge type number of the first series element
    /// </summary>
    /// <param name="expectedChargeTypeNumber"></param>
    IAssertWholesaleCalculationResultDocument HasChargeCode(string expectedChargeTypeNumber);

    /// <summary>
    /// Asserts the charge type of the first series element
    /// </summary>
    /// <param name="expectedChargeType"></param>
    IAssertWholesaleCalculationResultDocument HasChargeType(ChargeType expectedChargeType);

    /// <summary>
    /// Asserts the charge type owner of the first series element
    /// </summary>
    /// <param name="expectedChargeTypeOwner"></param>
    IAssertWholesaleCalculationResultDocument HasChargeTypeOwner(ActorNumber expectedChargeTypeOwner);

    /// <summary>
    /// Asserts grid area code of the first series element
    /// </summary>
    /// <param name="expectedGridAreaCode"></param>
    IAssertWholesaleCalculationResultDocument HasGridAreaCode(string expectedGridAreaCode);

    /// <summary>
    /// Asserts energy supplier number of the first series element
    /// </summary>
    /// <param name="expectedEnergySupplierNumber"></param>
    IAssertWholesaleCalculationResultDocument HasEnergySupplierNumber(ActorNumber expectedEnergySupplierNumber);

    /// <summary>
    /// Assets the product of the first series element
    /// </summary>
    IAssertWholesaleCalculationResultDocument HasProductCode(string expectedProductCode);

    /// <summary>
    /// Asserts the measure unit of the first series element
    /// </summary>
    /// <param name="expectedMeasurementUnit"></param>
    IAssertWholesaleCalculationResultDocument HasMeasurementUnit(MeasurementUnit expectedMeasurementUnit);

    /// <summary>
    /// Asserts the price measure unit of the first series element
    /// </summary>
    /// <param name="expectedPriceMeasurementUnit"></param>
    IAssertWholesaleCalculationResultDocument HasPriceMeasurementUnit(MeasurementUnit expectedPriceMeasurementUnit);

    /// <summary>
    /// Asserts the currency of the first series element
    /// </summary>
    /// <param name="expectedPriceUnit"></param>
    IAssertWholesaleCalculationResultDocument HasCurrency(Currency expectedPriceUnit);

    /// <summary>
    /// Asserts period of the first series element
    /// </summary>
    /// <param name="expectedPeriod"></param>
    IAssertWholesaleCalculationResultDocument HasPeriod(Period expectedPeriod);

    /// <summary>
    /// Asserts the resulution of the first series element
    /// </summary>
    /// <param name="resolution"></param>
    IAssertWholesaleCalculationResultDocument HasResolution(Resolution resolution);

    /// <summary>
    /// Asserts a point of the first series element
    /// </summary>
    /// <param name="expectedPosition"></param>
    /// <param name="expectedQuantity"></param>
    IAssertWholesaleCalculationResultDocument HasPositionAndQuantity(int expectedPosition, int expectedQuantity);

    /// <summary>
    /// Asserts the quality is present with the given code
    /// </summary>
    IAssertWholesaleCalculationResultDocument QualityIsPresentForPosition(int expectedPosition, string expectedQuantityQualityCode);

    /// <summary>
    /// Asserts the settlement method is not present
    /// </summary>
    IAssertWholesaleCalculationResultDocument SettlementMethodIsNotPresent();

    /// <summary>
    /// Asserts the settlement version is not present
    /// </summary>
    IAssertWholesaleCalculationResultDocument SettlementVersionIsNotPresent();
    #endregion
}
