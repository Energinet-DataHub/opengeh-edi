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
using Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.Asserts;
using FluentAssertions;
using NodaTime;

namespace Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.RSM018;

public class AssertMissingMeasurementXmlDocument : IAssertMissingMeasurementDocument
{
    private readonly AssertXmlDocument _documentAsserter;

    public AssertMissingMeasurementXmlDocument(AssertXmlDocument documentAsserter)
    {
        _documentAsserter = documentAsserter;
        _documentAsserter.HasValue("type", "D24");
    }

    public async Task<IAssertMissingMeasurementDocument> DocumentIsValidAsync()
    {
        await _documentAsserter.HasValidStructureAsync(DocumentType.ReminderOfMissingMeasureData).ConfigureAwait(false);
        return this;
    }

    #region Header
    public IAssertMissingMeasurementDocument HasMessageId(MessageId messageId)
    {
        _documentAsserter.HasValue("mRID", messageId.Value);
        return this;
    }

    public IAssertMissingMeasurementDocument HasBusinessReason(BusinessReason businessReason)
    {
        _documentAsserter.HasValue("process.processType", businessReason.Code);
        return this;
    }

    public IAssertMissingMeasurementDocument HasSenderId(ActorNumber actorNumber)
    {
        var attribute = "sender_MarketParticipant.mRID";
        _documentAsserter.HasValue(attribute, actorNumber.Value);
        _documentAsserter.HasAttribute(
            attribute,
            "codingScheme",
            ActorNumber.IsEic(actorNumber) ? "A01" : "A10");
        return this;
    }

    public IAssertMissingMeasurementDocument HasSenderRole(ActorRole actorRole)
    {
        _documentAsserter.HasValue("sender_MarketParticipant.marketRole.type", actorRole.Code);
        return this;
    }

    public IAssertMissingMeasurementDocument HasReceiverId(ActorNumber actorNumber)
    {
        var attribute = "receiver_MarketParticipant.mRID";
        _documentAsserter.HasValue(attribute, actorNumber.Value);
        _documentAsserter.HasAttribute(
            attribute,
            "codingScheme",
            ActorNumber.IsEic(actorNumber) ? "A01" : "A10");
        return this;
    }

    public IAssertMissingMeasurementDocument HasReceiverRole(ActorRole actorRole)
    {
        _documentAsserter.HasValue("receiver_MarketParticipant.marketRole.type", actorRole.Code);
        return this;
    }

    public IAssertMissingMeasurementDocument HasTimestamp(Instant timestamp)
    {
        _documentAsserter.HasValue("createdDateTime", timestamp.ToString());
        return this;
    }
    #endregion

    #region Series
    public IAssertMissingMeasurementDocument HasTransactionId(int seriesIndex, TransactionId expectedTransactionId)
    {
        _documentAsserter.HasValue($"Series[{seriesIndex}]/mRID", expectedTransactionId.Value);
        return this;
    }

    public IAssertMissingMeasurementDocument HasMeteringPointNumber(int seriesIndex, MeteringPointId meteringPointNumber)
    {
        _documentAsserter.HasValue($"Series[{seriesIndex}]/MarketEvaluationPoint/mRID", meteringPointNumber.Value);
        _documentAsserter.HasAttribute(
            $"Series[{seriesIndex}]/MarketEvaluationPoint/mRID",
            "codingScheme",
            "A10");
        return this;
    }

    public IAssertMissingMeasurementDocument HasMissingDate(int seriesIndex, Instant missingDate)
    {
        _documentAsserter.HasValue($"Series[{seriesIndex}]/request_DateAndOrTime.dateTime", missingDate.ToString());
        return this;
    }

    public IAssertMissingMeasurementDocument HasMissingData(
        IReadOnlyCollection<(MeteringPointId MeteringPointId, Instant Date)> missingData)
    {
        for (int i = 0; i < missingData.Count; i++)
        {
            missingData.Should()
                .ContainSingle(
                    data =>
                        data.MeteringPointId.Value == _documentAsserter.GetElement($"Series[{i + 1}]/MarketEvaluationPoint/mRID")!.Value
                        && data.Date.ToString() == _documentAsserter.GetElement($"Series[{i + 1}]/request_DateAndOrTime.dateTime")!.Value);
        }

        _documentAsserter.GetElements("Series").Should().HaveCount(missingData.Count);
        return this;
    }
    #endregion
}
