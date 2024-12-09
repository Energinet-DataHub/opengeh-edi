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
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.Asserts;
using FluentAssertions;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.RSM012;

public class AssertMeteredDateForMeasurementPointXmlDocument : IAssertMeteredDateForMeasurementPointDocumentDocument
{
    private readonly AssertXmlDocument _documentAsserter;

    public AssertMeteredDateForMeasurementPointXmlDocument(AssertXmlDocument documentAsserter)
    {
        _documentAsserter = documentAsserter;
        _documentAsserter.HasValue("type", "E66");
    }

    public IAssertMeteredDateForMeasurementPointDocumentDocument MessageIdExists()
    {
        _documentAsserter.ElementExists("mRID");
        return this;
    }

    public IAssertMeteredDateForMeasurementPointDocumentDocument HasBusinessReason(string expectedBusinessReasonCode)
    {
        _documentAsserter.HasValue("process.processType", expectedBusinessReasonCode);
        return this;
    }

    public IAssertMeteredDateForMeasurementPointDocumentDocument HasSenderId(string expectedSenderId, string expectedSchemeCode)
    {
        _documentAsserter.HasValue("sender_MarketParticipant.mRID", expectedSenderId);
        return this;
    }

    public IAssertMeteredDateForMeasurementPointDocumentDocument HasSenderRole(string expectedSenderRole)
    {
        _documentAsserter.HasValue("sender_MarketParticipant.marketRole.type", expectedSenderRole);
        return this;
    }

    public IAssertMeteredDateForMeasurementPointDocumentDocument HasReceiverId(
        string expectedReceiverId,
        string expectedSchemeCode)
    {
        _documentAsserter.HasValue("receiver_MarketParticipant.mRID", expectedReceiverId);
        return this;
    }

    public IAssertMeteredDateForMeasurementPointDocumentDocument HasReceiverRole(string expectedReceiverRole)
    {
        _documentAsserter.HasValue("receiver_MarketParticipant.marketRole.type", expectedReceiverRole);
        return this;
    }

    public IAssertMeteredDateForMeasurementPointDocumentDocument HasTimestamp(string expectedTimestamp)
    {
        _documentAsserter.HasValue("createdDateTime", expectedTimestamp);
        return this;
    }

    public IAssertMeteredDateForMeasurementPointDocumentDocument HasTransactionId(TransactionId expectedTransactionId)
    {
        _documentAsserter.HasValue("Series[1]/mRID", expectedTransactionId.Value);
        return this;
    }

    public IAssertMeteredDateForMeasurementPointDocumentDocument HasMeteringPointNumber(
        string expectedMeteringPointNumber,
        string expectedSchemeCode)
    {
        _documentAsserter.HasValue("Series[1]/marketEvaluationPoint.mRID", expectedMeteringPointNumber);
        return this;
    }

    public IAssertMeteredDateForMeasurementPointDocumentDocument HasMeteringPointType(string expectedMeteringPointType)
    {
        _documentAsserter.HasValue("Series[1]/marketEvaluationPoint.type", expectedMeteringPointType);
        return this;
    }

    public IAssertMeteredDateForMeasurementPointDocumentDocument HasOriginalTransactionIdReferenceId(
        string? expectedOriginalTransactionIdReferenceId)
    {
        if (expectedOriginalTransactionIdReferenceId == null)
        {
            _documentAsserter.IsNotPresent("Series[1]/originalTransactionIDReference_Series.mRID");
        }
        else
        {
            _documentAsserter.HasValue(
                "Series[1]/originalTransactionIDReference_Series.mRID",
                expectedOriginalTransactionIdReferenceId);
        }

        return this;
    }

    public IAssertMeteredDateForMeasurementPointDocumentDocument HasProduct(string expectedProduct)
    {
        _documentAsserter.HasValue("Series[1]/product", expectedProduct);
        return this;
    }

    public IAssertMeteredDateForMeasurementPointDocumentDocument HasQuantityMeasureUnit(string expectedQuantityMeasureUnit)
    {
        _documentAsserter.HasValue("Series[1]/quantity_Measure_Unit.name", expectedQuantityMeasureUnit);
        return this;
    }

    public IAssertMeteredDateForMeasurementPointDocumentDocument HasRegistrationDateTime(string expectedRegistrationDateTime)
    {
        _documentAsserter.HasValue("Series[1]/registration_DateAndOrTime.dateTime", expectedRegistrationDateTime);
        return this;
    }

    public IAssertMeteredDateForMeasurementPointDocumentDocument HasResolution(string expectedResolution)
    {
        _documentAsserter.HasValue("Series[1]/Period/resolution", expectedResolution);
        return this;
    }

    public IAssertMeteredDateForMeasurementPointDocumentDocument HasStartedDateTime(string expectedStartedDateTime)
    {
        _documentAsserter
            .HasValue("Series[1]/Period/timeInterval/start", expectedStartedDateTime);
        return this;
    }

    public IAssertMeteredDateForMeasurementPointDocumentDocument HasEndedDateTime(string expectedEndedDateTime)
    {
        _documentAsserter
            .HasValue("Series[1]/Period/timeInterval/end", expectedEndedDateTime);
        return this;
    }

    public IAssertMeteredDateForMeasurementPointDocumentDocument HasPoints(
        IReadOnlyCollection<(RequiredPointDocumentFields Rpdf, OptionalPointDocumentFields? Opdf)> expectedPoints)
    {
        var pointsInDocument = _documentAsserter
            .GetElements("Series[1]/Period/Point")!;

        var expectedPointsAsList = expectedPoints.ToList();

        pointsInDocument.Should().HaveSameCount(expectedPoints);

        for (var i = 0; i < expectedPointsAsList.Count; i++)
        {
            var (requiredPointDocumentFields, optionalPointDocumentFields) = expectedPointsAsList[i];

            _documentAsserter
                .HasValue($"Series[1]/Period/Point[{i + 1}]/position", requiredPointDocumentFields.Position.ToString());

            if (optionalPointDocumentFields == null)
            {
                continue;
            }

            if (optionalPointDocumentFields.Quantity != null)
            {
                _documentAsserter
                    .HasValue(
                        $"Series[1]/Period/Point[{i + 1}]/quantity",
                        optionalPointDocumentFields.Quantity!.ToString()!);
            }

            if (optionalPointDocumentFields.Quality != null)
            {
                _documentAsserter
                    .HasValue($"Series[1]/Period/Point[{i + 1}]/quality", optionalPointDocumentFields.Quality);
            }
        }

        return this;
    }

    public async Task<IAssertMeteredDateForMeasurementPointDocumentDocument> DocumentIsValidAsync()
    {
        await _documentAsserter.HasValidStructureAsync(DocumentType.NotifyValidatedMeasureData).ConfigureAwait(false);
        return this;
    }
}
