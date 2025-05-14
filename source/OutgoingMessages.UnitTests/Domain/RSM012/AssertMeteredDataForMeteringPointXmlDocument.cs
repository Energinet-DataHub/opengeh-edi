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

using System.Globalization;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.Asserts;
using FluentAssertions;

namespace Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.RSM012;

public class AssertMeteredDataForMeteringPointXmlDocument : IAssertMeteredDataForMeteringPointDocumentDocument
{
    private readonly AssertXmlDocument _documentAsserter;

    public AssertMeteredDataForMeteringPointXmlDocument(AssertXmlDocument documentAsserter)
    {
        _documentAsserter = documentAsserter;
        _documentAsserter.HasValue("type", "E66");
    }

    public IAssertMeteredDataForMeteringPointDocumentDocument MessageIdExists()
    {
        _documentAsserter.ElementExists("mRID");
        return this;
    }

    public IAssertMeteredDataForMeteringPointDocumentDocument HasBusinessReason(string expectedBusinessReasonCode)
    {
        _documentAsserter.HasValue("process.processType", expectedBusinessReasonCode);
        return this;
    }

    public IAssertMeteredDataForMeteringPointDocumentDocument HasSenderId(string expectedSenderId, string expectedSchemeCode)
    {
        _documentAsserter.HasValue("sender_MarketParticipant.mRID", expectedSenderId);
        return this;
    }

    public IAssertMeteredDataForMeteringPointDocumentDocument HasSenderRole(string expectedSenderRole)
    {
        _documentAsserter.HasValue("sender_MarketParticipant.marketRole.type", expectedSenderRole);
        return this;
    }

    public IAssertMeteredDataForMeteringPointDocumentDocument HasReceiverId(
        string expectedReceiverId,
        string expectedSchemeCode)
    {
        _documentAsserter.HasValue("receiver_MarketParticipant.mRID", expectedReceiverId);
        return this;
    }

    public IAssertMeteredDataForMeteringPointDocumentDocument HasReceiverRole(string expectedReceiverRole)
    {
        _documentAsserter.HasValue("receiver_MarketParticipant.marketRole.type", expectedReceiverRole);
        return this;
    }

    public IAssertMeteredDataForMeteringPointDocumentDocument HasTimestamp(string expectedTimestamp)
    {
        _documentAsserter.HasValue("createdDateTime", expectedTimestamp);
        return this;
    }

    public IAssertMeteredDataForMeteringPointDocumentDocument HasBusinessSectorType(
        string? expectedBusinessSectorType)
    {
        if (expectedBusinessSectorType == null)
        {
            _documentAsserter.IsNotPresent("businessSector.type");
        }
        else
        {
            _documentAsserter.HasValue("businessSector.type", expectedBusinessSectorType);
        }

        return this;
    }

    public IAssertMeteredDataForMeteringPointDocumentDocument TransactionIdExists(int seriesIndex)
    {
        _documentAsserter.ElementExists($"Series[{seriesIndex}]/mRID");
        return this;
    }

    public IAssertMeteredDataForMeteringPointDocumentDocument HasTransactionId(
        int seriesIndex,
        TransactionId expectedTransactionId)
    {
        _documentAsserter.HasValue($"Series[{seriesIndex}]/mRID", expectedTransactionId.Value);
        return this;
    }

    public IAssertMeteredDataForMeteringPointDocumentDocument HasMeteringPointNumber(
        int seriesIndex,
        string expectedMeteringPointNumber,
        string expectedSchemeCode)
    {
        _documentAsserter.HasValue($"Series[{seriesIndex}]/marketEvaluationPoint.mRID", expectedMeteringPointNumber);
        return this;
    }

    public IAssertMeteredDataForMeteringPointDocumentDocument HasMeteringPointType(
        int seriesIndex,
        MeteringPointType expectedMeteringPointType)
    {
        _documentAsserter.HasValue($"Series[{seriesIndex}]/marketEvaluationPoint.type", expectedMeteringPointType.Code);
        return this;
    }

    public IAssertMeteredDataForMeteringPointDocumentDocument HasOriginalTransactionIdReferenceId(
        int seriesIndex,
        string? expectedOriginalTransactionIdReferenceId)
    {
        if (expectedOriginalTransactionIdReferenceId == null)
        {
            _documentAsserter.IsNotPresent($"Series[{seriesIndex}]/originalTransactionIDReference_Series.mRID");
        }
        else
        {
            _documentAsserter.HasValue(
                $"Series[{seriesIndex}]/originalTransactionIDReference_Series.mRID",
                expectedOriginalTransactionIdReferenceId);
        }

        return this;
    }

    public IAssertMeteredDataForMeteringPointDocumentDocument HasProduct(int seriesIndex, string? expectedProduct)
    {
        if (expectedProduct is null)
        {
            _documentAsserter.IsNotPresent($"Series[{seriesIndex}]/product");
            return this;
        }

        _documentAsserter.HasValue($"Series[{seriesIndex}]/product", expectedProduct);
        return this;
    }

    public IAssertMeteredDataForMeteringPointDocumentDocument HasQuantityMeasureUnit(
        int seriesIndex,
        string expectedQuantityMeasureUnit)
    {
        _documentAsserter.HasValue($"Series[{seriesIndex}]/quantity_Measure_Unit.name", expectedQuantityMeasureUnit);
        return this;
    }

    public IAssertMeteredDataForMeteringPointDocumentDocument HasRegistrationDateTime(
        int seriesIndex,
        string? expectedRegistrationDateTime)
    {
        if (expectedRegistrationDateTime is null)
        {
            _documentAsserter.IsNotPresent($"Series[{seriesIndex}]/registration_DateAndOrTime.dateTime");
            return this;
        }

        _documentAsserter.HasValue(
            $"Series[{seriesIndex}]/registration_DateAndOrTime.dateTime",
            expectedRegistrationDateTime);
        return this;
    }

    public IAssertMeteredDataForMeteringPointDocumentDocument HasResolution(
        int seriesIndex,
        string expectedResolution)
    {
        _documentAsserter.HasValue($"Series[{seriesIndex}]/Period/resolution", expectedResolution);
        return this;
    }

    public IAssertMeteredDataForMeteringPointDocumentDocument HasStartedDateTime(
        int seriesIndex,
        string expectedStartedDateTime)
    {
        _documentAsserter
            .HasValue($"Series[{seriesIndex}]/Period/timeInterval/start", expectedStartedDateTime);
        return this;
    }

    public IAssertMeteredDataForMeteringPointDocumentDocument HasEndedDateTime(
        int seriesIndex,
        string expectedEndedDateTime)
    {
        _documentAsserter
            .HasValue($"Series[{seriesIndex}]/Period/timeInterval/end", expectedEndedDateTime);
        return this;
    }

    public IAssertMeteredDataForMeteringPointDocumentDocument HasInDomain(int seriesIndex, string? expectedInDomain)
    {
        if (expectedInDomain is null)
        {
            _documentAsserter.IsNotPresent($"Series[{seriesIndex}]/in_Domain.mRID");
            return this;
        }

        _documentAsserter.HasValue($"Series[{seriesIndex}]/in_Domain.mRID", expectedInDomain);

        return this;
    }

    public IAssertMeteredDataForMeteringPointDocumentDocument HasOutDomain(
        int seriesIndex,
        string? expectedOutDomain)
    {
        if (expectedOutDomain is null)
        {
            _documentAsserter.IsNotPresent($"Series[{seriesIndex}]/out_Domain.mRID");
            return this;
        }

        _documentAsserter.HasValue($"Series[{seriesIndex}]/out_Domain.mRID", expectedOutDomain);

        return this;
    }

    public IAssertMeteredDataForMeteringPointDocumentDocument HasPoints(
        int seriesIndex,
        IReadOnlyList<AssertPointDocumentFieldsInput> expectedPoints)
    {
        var pointsInDocument = _documentAsserter
            .GetElements($"Series[{seriesIndex}]/Period/Point")!;

        pointsInDocument.Should().HaveSameCount(expectedPoints);

        for (var i = 0; i < expectedPoints.Count; i++)
        {
            var (requiredPointDocumentFields, optionalPointDocumentFields) = expectedPoints[i];

            _documentAsserter
                .HasValue(
                    $"Series[{seriesIndex}]/Period/Point[{i + 1}]/position",
                    requiredPointDocumentFields.Position.ToString());

            if (optionalPointDocumentFields.Quantity.HasValue)
            {
                _documentAsserter
                    .HasValue(
                        $"Series[{seriesIndex}]/Period/Point[{i + 1}]/quantity",
                        optionalPointDocumentFields.Quantity.Value.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                AssertElementNotPresent($"Series[{seriesIndex}]/Period/Point[{i + 1}]/quantity");
            }

            if (optionalPointDocumentFields.Quality != null)
            {
                _documentAsserter
                    .HasValue(
                        $"Series[{seriesIndex}]/Period/Point[{i + 1}]/quality",
                        optionalPointDocumentFields.Quality.Code);
            }
            else
            {
                AssertElementNotPresent($"Series[{seriesIndex}]/Period/Point[{i + 1}]/quality");
            }
        }

        return this;

        void AssertElementNotPresent(string xpath)
        {
            _documentAsserter.IsNotPresent(xpath);
        }
    }

    public async Task<IAssertMeteredDataForMeteringPointDocumentDocument> DocumentIsValidAsync()
    {
        await _documentAsserter.HasValidStructureAsync(DocumentType.NotifyValidatedMeasureData).ConfigureAwait(false);
        return this;
    }
}
