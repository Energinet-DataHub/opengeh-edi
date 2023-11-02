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
using Energinet.DataHub.EDI.Process.Domain.OutgoingMessages;
using Energinet.DataHub.EDI.Process.Domain.Transactions.Aggregations;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.AggregationWholesaleResult;

/// <summary>
/// Assertion helper for aggregation result documents
/// </summary>
public interface IAssertAggregationWholesaleResultDocument
{
    /// <summary>
    /// Asserts message id
    /// </summary>
    /// <param name="expectedMessageId"></param>
    IAssertAggregationWholesaleResultDocument HasMessageId(string expectedMessageId);

    /// <summary>
    /// Assert sender id
    /// </summary>
    /// <param name="expectedSenderId"></param>
    IAssertAggregationWholesaleResultDocument HasSenderId(string expectedSenderId);

    /// <summary>
    /// Asserts receiver id
    /// </summary>
    /// <param name="expectedReceiverId"></param>
    IAssertAggregationWholesaleResultDocument HasReceiverId(string expectedReceiverId);

    /// <summary>
    /// Asserts time stamp
    /// </summary>
    /// <param name="expectedTimestamp"></param>
    IAssertAggregationWholesaleResultDocument HasTimestamp(string expectedTimestamp);

    /// <summary>
    /// Asserts transaction id
    /// </summary>
    /// <param name="expectedTransactionId"></param>
    IAssertAggregationWholesaleResultDocument HasTransactionId(Guid expectedTransactionId);

    /// <summary>
    /// Asserts grid area code
    /// </summary>
    /// <param name="expectedGridAreaCode"></param>
    IAssertAggregationWholesaleResultDocument HasGridAreaCode(string expectedGridAreaCode);

    /// <summary>
    /// Asserts balance supplier number
    /// </summary>
    /// <param name="expectedBalanceResponsibleNumber"></param>
    IAssertAggregationWholesaleResultDocument HasBalanceResponsibleNumber(string expectedBalanceResponsibleNumber);

    /// <summary>
    /// Asserts energy supplier number
    /// </summary>
    /// <param name="expectedEnergySupplierNumber"></param>
    IAssertAggregationWholesaleResultDocument HasEnergySupplierNumber(string expectedEnergySupplierNumber);

    /// <summary>
    /// Asserts product code
    /// </summary>
    /// <param name="expectedProductCode"></param>
    IAssertAggregationWholesaleResultDocument HasProductCode(string expectedProductCode);

    /// <summary>
    /// Asserts period
    /// </summary>
    /// <param name="expectedPeriod"></param>
    IAssertAggregationWholesaleResultDocument HasPeriod(Period expectedPeriod);

    /// <summary>
    /// Asserts a point
    /// </summary>
    /// <param name="position"></param>
    /// <param name="quantity"></param>
    IAssertAggregationWholesaleResultDocument HasPoint(int position, int quantity);

    /// <summary>
    /// Asserts document validity
    /// </summary>
    Task<IAssertAggregationWholesaleResultDocument> DocumentIsValidAsync();

    /// <summary>
    /// Asserts the settlement method is not present
    /// </summary>
    IAssertAggregationWholesaleResultDocument SettlementMethodIsNotPresent();

    /// <summary>
    /// Asserts the settlement version is not present
    /// </summary>
    IAssertAggregationWholesaleResultDocument SettlementVersionIsNotPresent();

    /// <summary>
    /// Asserts the energy supplier number is not present
    /// </summary>
    IAssertAggregationWholesaleResultDocument EnergySupplierNumberIsNotPresent();

    /// <summary>
    /// Asserts the balance responsible number is not present
    /// </summary>
    IAssertAggregationWholesaleResultDocument BalanceResponsibleNumberIsNotPresent();

    /// <summary>
    /// Asserts the quantity is not present
    /// </summary>
    /// <param name="position"></param>
    IAssertAggregationWholesaleResultDocument QuantityIsNotPresentForPosition(int position);

    /// <summary>
    /// Asserts the quality is not present
    /// </summary>
    /// <param name="position"></param>
    IAssertAggregationWholesaleResultDocument QualityIsNotPresentForPosition(int position);

    /// <summary>
    /// Asserts the process type.
    /// </summary>
    /// <param name="businessReason"></param>
    IAssertAggregationWholesaleResultDocument HasBusinessReason(BusinessReason businessReason);

    /// <summary>
    /// Asserts the SettlementVersion
    /// </summary>
    /// <param name="settlementVersion"></param>
    IAssertAggregationWholesaleResultDocument HasSettlementVersion(SettlementVersion settlementVersion);

    /// <summary>
    /// Asserts the OriginalTransactionIdReference
    /// </summary>
    /// <param name="originalTransactionIdReference"></param>
    IAssertAggregationWholesaleResultDocument HasOriginalTransactionIdReference(string originalTransactionIdReference);

    /// <summary>
    /// Asserts the settlement method
    /// </summary>
    IAssertAggregationWholesaleResultDocument HasSettlementMethod(SettlementType settlementMethod);
}
