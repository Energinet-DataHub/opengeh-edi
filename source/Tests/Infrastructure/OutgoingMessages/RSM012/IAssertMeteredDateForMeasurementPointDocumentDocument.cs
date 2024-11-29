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
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.RSM012;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.RSM012;

public interface IAssertMeteredDateForMeasurementPointDocumentDocument
{
    IAssertMeteredDateForMeasurementPointDocumentDocument HasMessageId(string expectedMessageId);

    IAssertMeteredDateForMeasurementPointDocumentDocument HasBusinessReason(string expectedBusinessReasonCode);

    IAssertMeteredDateForMeasurementPointDocumentDocument HasSenderId(string expectedSenderId, string expectedSchemeCode);

    IAssertMeteredDateForMeasurementPointDocumentDocument HasSenderRole(string expectedSenderRole);

    IAssertMeteredDateForMeasurementPointDocumentDocument HasReceiverId(string expectedReceiverId, string expectedSchemeCode);

    IAssertMeteredDateForMeasurementPointDocumentDocument HasReceiverRole(string expectedReceiverRole);

    IAssertMeteredDateForMeasurementPointDocumentDocument HasTimestamp(string expectedTimestamp);

    IAssertMeteredDateForMeasurementPointDocumentDocument HasTransactionId(TransactionId expectedTransactionId);

    IAssertMeteredDateForMeasurementPointDocumentDocument HasMeteringPointNumber(string expectedMeteringPointNumber, string expectedSchemeCode);

    IAssertMeteredDateForMeasurementPointDocumentDocument HasMeteringPointType(string expectedMeteringPointType);

    IAssertMeteredDateForMeasurementPointDocumentDocument HasOriginalTransactionIdReferenceId(string? expectedOriginalTransactionIdReferenceId);

    IAssertMeteredDateForMeasurementPointDocumentDocument HasProduct(string expectedProduct);

    IAssertMeteredDateForMeasurementPointDocumentDocument HasQuantityMeasureUnit(string expectedQuantityMeasureUnit);

    IAssertMeteredDateForMeasurementPointDocumentDocument HasRegistrationDateTime(string expectedRegistrationDateTime);

    IAssertMeteredDateForMeasurementPointDocumentDocument HasResolution(string expectedResolution);

    IAssertMeteredDateForMeasurementPointDocumentDocument HasStartedDateTime(string expectedStartedDateTime);

    IAssertMeteredDateForMeasurementPointDocumentDocument HasEndedDateTime(string expectedEndedDateTime);

    IAssertMeteredDateForMeasurementPointDocumentDocument HasPoints(IReadOnlyList<PointActivityRecord> expectedPoints);

    /// <summary>
    /// Asserts document validity
    /// </summary>
    Task<IAssertMeteredDateForMeasurementPointDocumentDocument> DocumentIsValidAsync();
}
