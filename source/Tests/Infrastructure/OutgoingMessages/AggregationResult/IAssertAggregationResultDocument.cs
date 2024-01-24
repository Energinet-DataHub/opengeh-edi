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

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.AggregationResult;

/// <summary>
/// Assertion helper for aggregation result documents
/// </summary>
public interface IAssertAggregationResultDocument
{
    /// <summary>
    /// Asserts message id
    /// </summary>
    /// <param name="expectedMessageId"></param>
    IAssertAggregationResultDocument HasMessageId(string expectedMessageId);

    /// <summary>
    /// Assert sender id
    /// </summary>
    /// <param name="expectedSenderId"></param>
    IAssertAggregationResultDocument HasSenderId(string expectedSenderId);

    /// <summary>
    /// Asserts receiver id
    /// </summary>
    /// <param name="expectedReceiverId"></param>
    IAssertAggregationResultDocument HasReceiverId(string expectedReceiverId);

    /// <summary>
    /// Asserts time stamp
    /// </summary>
    /// <param name="expectedTimestamp"></param>
    IAssertAggregationResultDocument HasTimestamp(string expectedTimestamp);

    /// <summary>
    /// Asserts transaction id
    /// </summary>
    /// <param name="expectedTransactionId"></param>
    IAssertAggregationResultDocument HasTransactionId(Guid expectedTransactionId);

    /// <summary>
    /// Asserts grid area code
    /// </summary>
    /// <param name="expectedGridAreaCode"></param>
    IAssertAggregationResultDocument HasGridAreaCode(string expectedGridAreaCode);

    /// <summary>
    /// Asserts balance supplier number
    /// </summary>
    /// <param name="expectedBalanceResponsibleNumber"></param>
    IAssertAggregationResultDocument HasBalanceResponsibleNumber(string expectedBalanceResponsibleNumber);

    /// <summary>
    /// Asserts energy supplier number
    /// </summary>
    /// <param name="expectedEnergySupplierNumber"></param>
    IAssertAggregationResultDocument HasEnergySupplierNumber(string expectedEnergySupplierNumber);

    /// <summary>
    /// Asserts product code
    /// </summary>
    /// <param name="expectedProductCode"></param>
    IAssertAggregationResultDocument HasProductCode(string expectedProductCode);

    /// <summary>
    /// Asserts period
    /// </summary>
    /// <param name="expectedPeriod"></param>
    IAssertAggregationResultDocument HasPeriod(Period expectedPeriod);

    /// <summary>
    /// Asserts a point
    /// </summary>
    /// <param name="position"></param>
    /// <param name="quantity"></param>
    IAssertAggregationResultDocument HasPoint(int position, int quantity);

    /// <summary>
    /// Asserts document validity
    /// </summary>
    Task<IAssertAggregationResultDocument> DocumentIsValidAsync();

    /// <summary>
    /// Asserts the settlement method is not present
    /// </summary>
    IAssertAggregationResultDocument SettlementMethodIsNotPresent();

    /// <summary>
    /// Asserts the settlement version is not present
    /// </summary>
    IAssertAggregationResultDocument SettlementVersionIsNotPresent();

    /// <summary>
    /// Asserts the energy supplier number is not present
    /// </summary>
    IAssertAggregationResultDocument EnergySupplierNumberIsNotPresent();

    /// <summary>
    /// Asserts the balance responsible number is not present
    /// </summary>
    IAssertAggregationResultDocument BalanceResponsibleNumberIsNotPresent();

    /// <summary>
    /// Asserts the quantity is not present
    /// </summary>
    /// <param name="position"></param>
    IAssertAggregationResultDocument QuantityIsNotPresentForPosition(int position);

    /// <summary>
    /// Asserts the quality is not present
    /// </summary>
    /// <param name="position"></param>
    IAssertAggregationResultDocument QualityIsNotPresentForPosition(int position);

    /// <summary>
    /// Asserts the process type.
    /// </summary>
    /// <param name="businessReason"></param>
    IAssertAggregationResultDocument HasBusinessReason(BusinessReason businessReason);

    /// <summary>
    /// Asserts the SettlementVersion
    /// </summary>
    /// <param name="settlementVersion"></param>
    IAssertAggregationResultDocument HasSettlementVersion(SettlementVersion settlementVersion);

    /// <summary>
    /// Asserts the OriginalTransactionIdReference
    /// </summary>
    /// <param name="originalTransactionIdReference"></param>
    IAssertAggregationResultDocument HasOriginalTransactionIdReference(string originalTransactionIdReference);

    /// <summary>
    /// Asserts the settlement method
    /// </summary>
    IAssertAggregationResultDocument HasSettlementMethod(SettlementType settlementMethod);

    /// <summary>
    ///     Asserts the quality is present with the given code
    /// </summary>
    IAssertAggregationResultDocument QualityIsPresentForPosition(int position, string quantityQualityCode);

    /// <summary>
    ///     Asserts the calculation result version is present with the given number
    /// </summary>
    IAssertAggregationResultDocument HasCalculationResultVersion(int version);
}
