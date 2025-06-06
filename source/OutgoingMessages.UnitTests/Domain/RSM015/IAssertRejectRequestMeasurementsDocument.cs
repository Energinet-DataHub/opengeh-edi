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

namespace Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.RSM015;

/// <summary>
/// Assertion helper for aggregation result documents
/// </summary>
public interface IAssertRejectRequestMeasurementsDocument
{
    /// <summary>
    /// Asserts message id
    /// </summary>
    /// <param name="expectedMessageId"></param>
    IAssertRejectRequestMeasurementsDocument HasMessageId(string expectedMessageId);

    /// <summary>
    /// Assert message id exists
    /// </summary>
    IAssertRejectRequestMeasurementsDocument MessageIdExists();

    /// <summary>
    /// Assert sender id
    /// </summary>
    /// <param name="expectedSenderId"></param>
    IAssertRejectRequestMeasurementsDocument HasSenderId(ActorNumber expectedSenderId);

    /// <summary>
    /// Assert sender role
    /// </summary>
    /// <param name="expectedSenderRole"></param>
    IAssertRejectRequestMeasurementsDocument HasSenderRole(ActorRole expectedSenderRole);

    /// <summary>
    /// Asserts receiver id
    /// </summary>
    /// <param name="expectedReceiverId"></param>
    IAssertRejectRequestMeasurementsDocument HasReceiverId(ActorNumber expectedReceiverId);

    /// <summary>
    /// Assert sender role
    /// </summary>
    /// <param name="expectedReceiverRole"></param>
    IAssertRejectRequestMeasurementsDocument HasReceiverRole(ActorRole expectedReceiverRole);

    /// <summary>
    /// Asserts time stamp
    /// </summary>
    /// <param name="expectedTimestamp"></param>
    IAssertRejectRequestMeasurementsDocument HasTimestamp(Instant expectedTimestamp);

    /// <summary>
    /// Asserts reason code
    /// </summary>
    /// <param name="reasonCode"></param>
    IAssertRejectRequestMeasurementsDocument HasReasonCode(ReasonCode reasonCode);

    /// <summary>
    /// Asserts document validity
    /// </summary>
    Task<IAssertRejectRequestMeasurementsDocument> DocumentIsValidAsync();

    /// <summary>
    /// Asserts the business reason
    /// </summary>
    /// <param name="businessReason"></param>
    IAssertRejectRequestMeasurementsDocument HasBusinessReason(BusinessReason businessReason);

    /// <summary>
    /// Asserts transaction id
    /// </summary>
    /// <param name="expectedTransactionId"></param>
    IAssertRejectRequestMeasurementsDocument HasTransactionId(TransactionId expectedTransactionId);

    /// <summary>
    /// Asserts transaction id exists
    /// </summary>
    IAssertRejectRequestMeasurementsDocument TransactionIdExists();

    /// <summary>
    /// Asserts serie reason code
    /// </summary>
    /// <param name="expectedSerieReasonCode"></param>
    IAssertRejectRequestMeasurementsDocument HasSerieReasonCode(string expectedSerieReasonCode);

    /// <summary>
    /// Asserts serie reason message
    /// </summary>
    /// <param name="expectedSerieReasonMessage"></param>
    IAssertRejectRequestMeasurementsDocument HasSerieReasonMessage(string expectedSerieReasonMessage);

    /// <summary>
    /// Asserts original transaction id
    /// </summary>
    /// <param name="expectedOriginalTransactionId"></param>
    IAssertRejectRequestMeasurementsDocument HasOriginalTransactionId(TransactionId expectedOriginalTransactionId);

    /// <summary>
    /// Asserts metering point id
    /// </summary>
    /// <param name="expectedMeteringPointId"></param>
    IAssertRejectRequestMeasurementsDocument HasMeteringPointId(
        MeteringPointId expectedMeteringPointId);
}
