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

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.RSM012;

public interface IAssertMeteredDateForMeteringPointDocumentDocument
{
    IAssertMeteredDateForMeteringPointDocumentDocument MessageIdExists();

    IAssertMeteredDateForMeteringPointDocumentDocument HasBusinessReason(string expectedBusinessReasonCode);

    IAssertMeteredDateForMeteringPointDocumentDocument HasSenderId(string expectedSenderId, string expectedSchemeCode);

    IAssertMeteredDateForMeteringPointDocumentDocument HasSenderRole(string expectedSenderRole);

    IAssertMeteredDateForMeteringPointDocumentDocument HasReceiverId(string expectedReceiverId, string expectedSchemeCode);

    IAssertMeteredDateForMeteringPointDocumentDocument HasReceiverRole(string expectedReceiverRole);

    IAssertMeteredDateForMeteringPointDocumentDocument HasTimestamp(string expectedTimestamp);

    IAssertMeteredDateForMeteringPointDocumentDocument HasBusinessSectorType(string? expectedBusinessSectorType);

    IAssertMeteredDateForMeteringPointDocumentDocument HasTransactionId(
        int seriesIndex,
        TransactionId expectedTransactionId);

    IAssertMeteredDateForMeteringPointDocumentDocument HasMeteringPointNumber(
        int seriesIndex,
        string expectedMeteringPointNumber,
        string expectedSchemeCode);

    IAssertMeteredDateForMeteringPointDocumentDocument HasMeteringPointType(
        int seriesIndex,
        string expectedMeteringPointType);

    IAssertMeteredDateForMeteringPointDocumentDocument HasOriginalTransactionIdReferenceId(
        int seriesIndex,
        string? expectedOriginalTransactionIdReferenceId);

    IAssertMeteredDateForMeteringPointDocumentDocument HasProduct(int seriesIndex, string? expectedProduct);

    IAssertMeteredDateForMeteringPointDocumentDocument HasQuantityMeasureUnit(
        int seriesIndex,
        string expectedQuantityMeasureUnit);

    IAssertMeteredDateForMeteringPointDocumentDocument HasRegistrationDateTime(
        int seriesIndex,
        string? expectedRegistrationDateTime);

    IAssertMeteredDateForMeteringPointDocumentDocument HasResolution(int seriesIndex, string expectedResolution);

    IAssertMeteredDateForMeteringPointDocumentDocument HasStartedDateTime(
        int seriesIndex,
        string expectedStartedDateTime);

    IAssertMeteredDateForMeteringPointDocumentDocument HasEndedDateTime(
        int seriesIndex,
        string expectedEndedDateTime);

    IAssertMeteredDateForMeteringPointDocumentDocument HasInDomain(
        int seriesIndex,
        string? expectedInDomain);

    IAssertMeteredDateForMeteringPointDocumentDocument HasOutDomain(
        int seriesIndex,
        string? expectedOutDomain);

    IAssertMeteredDateForMeteringPointDocumentDocument HasPoints(
        int seriesIndex,
        IReadOnlyList<AssertPointDocumentFieldsInput> expectedPoints);

    /// <summary>
    /// Asserts document validity
    /// </summary>
    Task<IAssertMeteredDateForMeteringPointDocumentDocument> DocumentIsValidAsync();
}
