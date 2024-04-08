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
using NodaTime;

namespace OutgoingMessages.Application.Tests.MarketDocuments.RejectRequestAggregatedMeasureData;

/// <summary>
/// Assertion helper for aggregation result documents
/// </summary>
public interface IAssertRejectRequestAggregatedMeasureDataDocument
{
    /// <summary>
    /// Asserts message id
    /// </summary>
    /// <param name="expectedMessageId"></param>
    IAssertRejectRequestAggregatedMeasureDataDocument HasMessageId(string expectedMessageId);

    /// <summary>
    /// Assert sender id
    /// </summary>
    /// <param name="expectedSenderId"></param>
    IAssertRejectRequestAggregatedMeasureDataDocument HasSenderId(string expectedSenderId);

    /// <summary>
    /// Asserts receiver id
    /// </summary>
    /// <param name="expectedReceiverId"></param>
    IAssertRejectRequestAggregatedMeasureDataDocument HasReceiverId(string expectedReceiverId);

    /// <summary>
    /// Asserts time stamp
    /// </summary>
    /// <param name="expectedTimestamp"></param>
    IAssertRejectRequestAggregatedMeasureDataDocument HasTimestamp(Instant expectedTimestamp);

    /// <summary>
    /// Asserts reason code
    /// </summary>
    /// <param name="reasonCode"></param>
    IAssertRejectRequestAggregatedMeasureDataDocument HasReasonCode(string reasonCode);

    /// <summary>
    /// Asserts document validity
    /// </summary>
    Task<IAssertRejectRequestAggregatedMeasureDataDocument> DocumentIsValidAsync();

    /// <summary>
    /// Asserts the business reason
    /// </summary>
    /// <param name="businessReason"></param>
    IAssertRejectRequestAggregatedMeasureDataDocument HasBusinessReason(BusinessReason businessReason);

    /// <summary>
    /// Asserts transaction id
    /// </summary>
    /// <param name="expectedTransactionId"></param>
    IAssertRejectRequestAggregatedMeasureDataDocument HasTransactionId(Guid expectedTransactionId);

    /// <summary>
    /// Asserts serie reason code
    /// </summary>
    /// <param name="expectedSerieReasonCode"></param>
    IAssertRejectRequestAggregatedMeasureDataDocument HasSerieReasonCode(string expectedSerieReasonCode);

    /// <summary>
    /// Asserts serie reason message
    /// </summary>
    /// <param name="expectedSerieReasonMessage"></param>
    IAssertRejectRequestAggregatedMeasureDataDocument HasSerieReasonMessage(string expectedSerieReasonMessage);

    /// <summary>
    /// Asserts original transaction id
    /// </summary>
    /// <param name="expectedOriginalTransactionId"></param>
    IAssertRejectRequestAggregatedMeasureDataDocument HasOriginalTransactionId(string expectedOriginalTransactionId);
}
