﻿// Copyright 2020 Energinet DataHub A/S
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

public interface IAssertMeteredDateForMeasurementPointDocumentDocument
{
    IAssertMeteredDateForMeasurementPointDocumentDocument MessageIdExists();

    IAssertMeteredDateForMeasurementPointDocumentDocument HasBusinessReason(string expectedBusinessReasonCode);

    IAssertMeteredDateForMeasurementPointDocumentDocument HasSenderId(string expectedSenderId, string expectedSchemeCode);

    IAssertMeteredDateForMeasurementPointDocumentDocument HasSenderRole(string expectedSenderRole);

    IAssertMeteredDateForMeasurementPointDocumentDocument HasReceiverId(string expectedReceiverId, string expectedSchemeCode);

    IAssertMeteredDateForMeasurementPointDocumentDocument HasReceiverRole(string expectedReceiverRole);

    IAssertMeteredDateForMeasurementPointDocumentDocument HasTimestamp(string expectedTimestamp);

    IAssertMeteredDateForMeasurementPointDocumentDocument HasBusinessSectorType(string? expectedBusinessSectorType);

    IAssertMeteredDateForMeasurementPointDocumentDocument HasTransactionId(
        int seriesIndex,
        TransactionId expectedTransactionId);

    IAssertMeteredDateForMeasurementPointDocumentDocument HasMeteringPointNumber(
        int seriesIndex,
        string expectedMeteringPointNumber,
        string expectedSchemeCode);

    IAssertMeteredDateForMeasurementPointDocumentDocument HasMeteringPointType(
        int seriesIndex,
        string expectedMeteringPointType);

    IAssertMeteredDateForMeasurementPointDocumentDocument HasOriginalTransactionIdReferenceId(
        int seriesIndex,
        string? expectedOriginalTransactionIdReferenceId);

    IAssertMeteredDateForMeasurementPointDocumentDocument HasProduct(int seriesIndex, string? expectedProduct);

    IAssertMeteredDateForMeasurementPointDocumentDocument HasQuantityMeasureUnit(
        int seriesIndex,
        string expectedQuantityMeasureUnit);

    IAssertMeteredDateForMeasurementPointDocumentDocument HasRegistrationDateTime(
        int seriesIndex,
        string? expectedRegistrationDateTime);

    IAssertMeteredDateForMeasurementPointDocumentDocument HasResolution(int seriesIndex, string expectedResolution);

    IAssertMeteredDateForMeasurementPointDocumentDocument HasStartedDateTime(
        int seriesIndex,
        string expectedStartedDateTime);

    IAssertMeteredDateForMeasurementPointDocumentDocument HasEndedDateTime(
        int seriesIndex,
        string expectedEndedDateTime);

    IAssertMeteredDateForMeasurementPointDocumentDocument HasInDomain(
        int seriesIndex,
        string? expectedInDomain);

    IAssertMeteredDateForMeasurementPointDocumentDocument HasOutDomain(
        int seriesIndex,
        string? expectedOutDomain);

    IAssertMeteredDateForMeasurementPointDocumentDocument HasPoints(
        int seriesIndex,
        IReadOnlyList<AssertPointDocumentFieldsInput> expectedPoints);

    /// <summary>
    /// Asserts document validity
    /// </summary>
    Task<IAssertMeteredDateForMeasurementPointDocumentDocument> DocumentIsValidAsync();
}
