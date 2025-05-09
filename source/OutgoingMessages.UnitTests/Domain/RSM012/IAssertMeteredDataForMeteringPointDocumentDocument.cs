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

namespace Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.RSM012;

public interface IAssertMeteredDataForMeteringPointDocumentDocument
{
    IAssertMeteredDataForMeteringPointDocumentDocument MessageIdExists();

    IAssertMeteredDataForMeteringPointDocumentDocument HasBusinessReason(string expectedBusinessReasonCode);

    IAssertMeteredDataForMeteringPointDocumentDocument HasSenderId(string expectedSenderId, string expectedSchemeCode);

    IAssertMeteredDataForMeteringPointDocumentDocument HasSenderRole(string expectedSenderRole);

    IAssertMeteredDataForMeteringPointDocumentDocument HasReceiverId(string expectedReceiverId, string expectedSchemeCode);

    IAssertMeteredDataForMeteringPointDocumentDocument HasReceiverRole(string expectedReceiverRole);

    IAssertMeteredDataForMeteringPointDocumentDocument HasTimestamp(string expectedTimestamp);

    IAssertMeteredDataForMeteringPointDocumentDocument HasBusinessSectorType(string? expectedBusinessSectorType);

    IAssertMeteredDataForMeteringPointDocumentDocument TransactionIdExists(int seriesIndex);

    IAssertMeteredDataForMeteringPointDocumentDocument HasTransactionId(
        int seriesIndex,
        TransactionId expectedTransactionId);

    IAssertMeteredDataForMeteringPointDocumentDocument HasMeteringPointNumber(
        int seriesIndex,
        string expectedMeteringPointNumber,
        string expectedSchemeCode);

    IAssertMeteredDataForMeteringPointDocumentDocument HasMeteringPointType(
        int seriesIndex,
        MeteringPointType expectedMeteringPointType);

    IAssertMeteredDataForMeteringPointDocumentDocument HasOriginalTransactionIdReferenceId(
        int seriesIndex,
        string? expectedOriginalTransactionIdReferenceId);

    IAssertMeteredDataForMeteringPointDocumentDocument HasProduct(int seriesIndex, string? expectedProduct);

    IAssertMeteredDataForMeteringPointDocumentDocument HasQuantityMeasureUnit(
        int seriesIndex,
        string expectedQuantityMeasureUnit);

    IAssertMeteredDataForMeteringPointDocumentDocument HasRegistrationDateTime(
        int seriesIndex,
        string? expectedRegistrationDateTime);

    IAssertMeteredDataForMeteringPointDocumentDocument HasResolution(int seriesIndex, string expectedResolution);

    IAssertMeteredDataForMeteringPointDocumentDocument HasStartedDateTime(
        int seriesIndex,
        string expectedStartedDateTime);

    IAssertMeteredDataForMeteringPointDocumentDocument HasEndedDateTime(
        int seriesIndex,
        string expectedEndedDateTime);

    IAssertMeteredDataForMeteringPointDocumentDocument HasInDomain(
        int seriesIndex,
        string? expectedInDomain);

    IAssertMeteredDataForMeteringPointDocumentDocument HasOutDomain(
        int seriesIndex,
        string? expectedOutDomain);

    IAssertMeteredDataForMeteringPointDocumentDocument HasPoints(
        int seriesIndex,
        IReadOnlyList<AssertPointDocumentFieldsInput> expectedPoints);

    /// <summary>
    /// Asserts document validity
    /// </summary>
    Task<IAssertMeteredDataForMeteringPointDocumentDocument> DocumentIsValidAsync();
}
