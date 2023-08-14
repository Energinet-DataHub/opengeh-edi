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
using Domain.OutgoingMessages;
using Period = Domain.Transactions.Aggregations.Period;

namespace Tests.Infrastructure.OutgoingMessages.RejectRequestAggregatedMeasureData;

/// <summary>
/// Assertion helper for aggregation result documents
/// </summary>
public interface IAssertRejectedAggregatedMeasureDataResultDocument
{
    /// <summary>
    /// Asserts message id
    /// </summary>
    /// <param name="expectedMessageId"></param>
    IAssertRejectedAggregatedMeasureDataResultDocument HasMessageId(string expectedMessageId);

    /// <summary>
    /// Assert sender id
    /// </summary>
    /// <param name="expectedSenderId"></param>
    IAssertRejectedAggregatedMeasureDataResultDocument HasSenderId(string expectedSenderId);

    /// <summary>
    /// Asserts receiver id
    /// </summary>
    /// <param name="expectedReceiverId"></param>
    IAssertRejectedAggregatedMeasureDataResultDocument HasReceiverId(string expectedReceiverId);

    /// <summary>
    /// Asserts time stamp
    /// </summary>
    /// <param name="expectedTimestamp"></param>
    IAssertRejectedAggregatedMeasureDataResultDocument HasTimestamp(string expectedTimestamp);

    /// <summary>
    /// Asserts transaction id
    /// </summary>
    /// <param name="expectedTransactionId"></param>
    IAssertRejectedAggregatedMeasureDataResultDocument HasTransactionId(Guid expectedTransactionId);

    /// <summary>
    /// Asserts grid area code
    /// </summary>
    /// <param name="expectedGridAreaCode"></param>
    IAssertRejectedAggregatedMeasureDataResultDocument HasGridAreaCode(string expectedGridAreaCode);

    /// <summary>
    /// Asserts balance supplier number
    /// </summary>
    /// <param name="expectedBalanceResponsibleNumber"></param>
    IAssertRejectedAggregatedMeasureDataResultDocument HasBalanceResponsibleNumber(string expectedBalanceResponsibleNumber);

    /// <summary>
    /// Asserts energy supplier number
    /// </summary>
    /// <param name="expectedEnergySupplierNumber"></param>
    IAssertRejectedAggregatedMeasureDataResultDocument HasEnergySupplierNumber(string expectedEnergySupplierNumber);

    /// <summary>
    /// Asserts product code
    /// </summary>
    /// <param name="expectedProductCode"></param>
    IAssertRejectedAggregatedMeasureDataResultDocument HasProductCode(string expectedProductCode);

    /// <summary>
    /// Asserts period
    /// </summary>
    /// <param name="expectedPeriod"></param>
    IAssertRejectedAggregatedMeasureDataResultDocument HasPeriod(Period expectedPeriod);

    /// <summary>
    /// Asserts a point
    /// </summary>
    /// <param name="position"></param>
    /// <param name="quantity"></param>
    IAssertRejectedAggregatedMeasureDataResultDocument HasPoint(int position, int quantity);

    /// <summary>
    /// Asserts document validity
    /// </summary>
    Task<IAssertRejectedAggregatedMeasureDataResultDocument> DocumentIsValidAsync();

    /// <summary>
    /// Asserts the settlement method is not present
    /// </summary>
    IAssertRejectedAggregatedMeasureDataResultDocument SettlementMethodIsNotPresent();

    /// <summary>
    /// Asserts the energy supplier number is not present
    /// </summary>
    IAssertRejectedAggregatedMeasureDataResultDocument EnergySupplierNumberIsNotPresent();

    /// <summary>
    /// Asserts the balance responsible number is not present
    /// </summary>
    IAssertRejectedAggregatedMeasureDataResultDocument BalanceResponsibleNumberIsNotPresent();

    /// <summary>
    /// Asserts the quantity is not present
    /// </summary>
    /// <param name="position"></param>
    IAssertRejectedAggregatedMeasureDataResultDocument QuantityIsNotPresentForPosition(int position);

    /// <summary>
    /// Asserts the quality is not present
    /// </summary>
    /// <param name="position"></param>
    IAssertRejectedAggregatedMeasureDataResultDocument QualityIsNotPresentForPosition(int position);

    /// <summary>
    /// Asserts the process type.
    /// </summary>
    /// <param name="businessReason"></param>
    IAssertRejectedAggregatedMeasureDataResultDocument HasBusinessReason(BusinessReason businessReason);
}
