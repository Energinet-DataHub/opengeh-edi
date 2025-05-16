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
        throw new NotImplementedException();
    }

    public IAssertMissingMeasurementDocument HasBusinessReason(BusinessReason businessReason)
    {
        throw new NotImplementedException();
    }

    public IAssertMissingMeasurementDocument HasSenderId(ActorNumber actorNumber)
    {
        throw new NotImplementedException();
    }

    public IAssertMissingMeasurementDocument HasSenderRole(ActorRole actorRole)
    {
        throw new NotImplementedException();
    }

    public IAssertMissingMeasurementDocument HasReceiverId(ActorNumber actorNumber)
    {
        throw new NotImplementedException();
    }

    public IAssertMissingMeasurementDocument HasReceiverRole(ActorRole actorRole)
    {
        throw new NotImplementedException();
    }

    public IAssertMissingMeasurementDocument HasTimestamp(Instant timestamp)
    {
        throw new NotImplementedException();
    }

    public IAssertMissingMeasurementDocument HasTransactionId(int seriesIndex, TransactionId expectedTransactionId)
    {
        throw new NotImplementedException();
    }

    public IAssertMissingMeasurementDocument HasMeteringPointNumber(int seriesIndex, string meteringPointNumber)
    {
        throw new NotImplementedException();
    }

    public IAssertMissingMeasurementDocument HasMissingDate(int seriesIndex, Instant missingDate)
    {
        throw new NotImplementedException();
    }
}
