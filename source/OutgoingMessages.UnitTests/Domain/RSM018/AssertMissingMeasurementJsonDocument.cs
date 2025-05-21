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

using System.Text.Json;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.Schemas.Cim.Json;
using FluentAssertions;
using Json.Schema;
using NodaTime;

namespace Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.RSM018;

public class AssertMissingMeasurementJsonDocument : IAssertMissingMeasurementDocument
{
    private readonly JsonSchemaProvider _schemas = new(new CimJsonSchemas());
    private readonly JsonDocument _document;
    private readonly JsonElement _root;

    public AssertMissingMeasurementJsonDocument(Stream stream)
    {
        _document = JsonDocument.Parse(stream);
        _root = _document.RootElement.GetProperty("ReminderOfMissingMeasureData_MarketDocument");
        _root.TryGetProperty("type", out _).Should().BeTrue();
        _root.GetProperty("businessSector.type")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be("23");
    }

    public async Task<IAssertMissingMeasurementDocument> DocumentIsValidAsync()
    {
        var schema = await _schemas
            .GetSchemaAsync<JsonSchema>("REMINDEROFMISSINGMEASUREDATA", "0", CancellationToken.None)
            .ConfigureAwait(false);

        var validationOptions = new EvaluationOptions { OutputFormat = OutputFormat.List };
        var validationResult = schema!.Evaluate(_document, validationOptions);
        var errors = validationResult.Details.Where(detail => detail.HasErrors)
            .Select(x => (x.InstanceLocation, x.EvaluationPath, x.Errors))
            .SelectMany(p => p.Errors!.Values.Select(e =>
                $"==> '{p.InstanceLocation}' does not adhere to '{p.EvaluationPath}' with error: {e}\n"));

        validationResult.IsValid.Should()
            .BeTrue(
                $"because document should be valid. Validation errors:{Environment.NewLine}{{0}}",
                string.Join("\n", errors));

        return this;
    }

    public IAssertMissingMeasurementDocument HasMessageId(MessageId messageId)
    {
        _root.GetProperty("mRID").GetString().Should().Be(messageId.Value);
        return this;
    }

    public IAssertMissingMeasurementDocument HasBusinessReason(BusinessReason businessReason)
    {
        _root.GetProperty("process.processType")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(businessReason.Code);

        return this;
    }

    public IAssertMissingMeasurementDocument HasSenderId(ActorNumber actorNumber)
    {
        var sender = _root.GetProperty("sender_MarketParticipant.mRID");
        sender
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(actorNumber.Value);
        sender
            .GetProperty("codingScheme")
            .GetString()
            .Should()
            .Be(ActorNumber.IsEic(actorNumber) ? "A01" : "A10");

        return this;
    }

    public IAssertMissingMeasurementDocument HasSenderRole(ActorRole actorRole)
    {
        _root.GetProperty("sender_MarketParticipant.marketRole.type")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(actorRole.Code);

        return this;
    }

    public IAssertMissingMeasurementDocument HasReceiverId(ActorNumber actorNumber)
    {
        var receiver = _root.GetProperty("receiver_MarketParticipant.mRID");
        receiver
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(actorNumber.Value);
        receiver
            .GetProperty("codingScheme")
            .GetString()
            .Should()
            .Be(ActorNumber.IsEic(actorNumber) ? "A01" : "A10");

        return this;
    }

    public IAssertMissingMeasurementDocument HasReceiverRole(ActorRole actorRole)
    {
        _root.GetProperty("receiver_MarketParticipant.marketRole.type")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(actorRole.Code);

        return this;
    }

    public IAssertMissingMeasurementDocument HasTimestamp(Instant timestamp)
    {
        _root.GetProperty("createdDateTime")
            .GetString()
            .Should()
            .Be(timestamp.ToString());

        return this;
    }

    public IAssertMissingMeasurementDocument HasTransactionId(int seriesIndex, TransactionId expectedTransactionId)
    {
        GetTimeSeriesElement(seriesIndex)
            .GetProperty("mRID")
            .GetString()
            .Should()
            .Be(expectedTransactionId.Value);

        return this;
    }

    public IAssertMissingMeasurementDocument HasMeteringPointNumber(int seriesIndex, MeteringPointId meteringPointNumber)
    {
        GetTimeSeriesElement(seriesIndex)
            .GetProperty("MarketEvaluationPoint")
            .EnumerateArray()
            .Single()
            .GetProperty("mRID")
            .GetProperty("value")
            .ToString()
            .Should()
            .Be(meteringPointNumber.Value);

        return this;
    }

    public IAssertMissingMeasurementDocument HasMissingDate(int seriesIndex, Instant missingDate)
    {
        GetTimeSeriesElement(seriesIndex)
            .GetProperty("request_DateAndOrTime.dateTime")
            .GetString()
            .Should()
            .Be(missingDate.ToString());

        return this;
    }

    private JsonElement GetTimeSeriesElement(int seriesIndex) =>
        _root.GetProperty("Series").EnumerateArray().ToList()[seriesIndex - 1];
}
