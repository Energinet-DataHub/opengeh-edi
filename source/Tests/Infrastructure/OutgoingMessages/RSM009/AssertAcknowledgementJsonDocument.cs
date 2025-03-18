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
using Energinet.DataHub.EDI.IncomingMessages.Domain.Schemas.Cim.Json;
using FluentAssertions;
using Json.Schema;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.RSM009;

public class AssertAcknowledgementJsonDocument : IAssertAcknowledgementDocument
{
    private readonly JsonSchemaProvider _schemas = new(new CimJsonSchemas());
    private readonly JsonDocument _document;
    private readonly JsonElement _root;

    public AssertAcknowledgementJsonDocument(Stream stream)
    {
        _document = JsonDocument.Parse(stream);
        _root = _document.RootElement.GetProperty("Acknowledgement_MarketDocument");
    }

    public IAssertAcknowledgementDocument HasMessageId(MessageId messageId)
    {
        _root.GetProperty("mRID").GetString().Should().Be(messageId.Value);
        return this;
    }

    public IAssertAcknowledgementDocument HasSenderId(ActorNumber senderId)
    {
        _root.GetProperty("sender_MarketParticipant.mRID")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(senderId.Value);
        return this;
    }

    public IAssertAcknowledgementDocument HasSenderRole(ActorRole senderRole)
    {
        _root.GetProperty("sender_MarketParticipant.marketRole.type")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(senderRole.Code);
        return this;
    }

    public IAssertAcknowledgementDocument HasReceiverId(ActorNumber receiverId)
    {
        _root.GetProperty("receiver_MarketParticipant.mRID")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(receiverId.Value);
        return this;
    }

    public IAssertAcknowledgementDocument HasReceiverRole(ActorRole receiverRole)
    {
        _root.GetProperty("receiver_MarketParticipant.marketRole.type")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(receiverRole.Code);
        return this;
    }

    public IAssertAcknowledgementDocument HasReasonCode(ReasonCode reasonCode)
    {
        _root.GetProperty("reason.code")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(reasonCode.Code);
        return this;
    }

    public IAssertAcknowledgementDocument HasOriginalMessageId(MessageId originalMessageId)
    {
        _root.GetProperty("received_MarketDocument.mRID")
            .GetString()
            .Should()
            .Be(originalMessageId.Value);
        return this;
    }

    public Task<IAssertAcknowledgementDocument> DocumentIsValidAsync()
    {
        var schema = JsonSchema.FromFile(@"Infrastructure\OutgoingMessages\Schemas\Json\Acknowledgement-assembly-model.schema.json");

        schema.Should().NotBeNull("Cannot validate document without a schema");

        var validationOptions = new EvaluationOptions { OutputFormat = OutputFormat.List };
        var validationResult = schema!.Evaluate(_document, validationOptions);
        var errors = validationResult.Details.Where(detail => detail.HasErrors)
            .Select(x => (x.InstanceLocation, x.EvaluationPath, x.Errors))
            .SelectMany(
                p => p.Errors!.Values.Select(
                    e => $"==> '{p.InstanceLocation}' does not adhere to '{p.EvaluationPath}' with error: {e}\n"));

        validationResult.IsValid.Should().BeTrue($"because document should be valid. Validation errors:{Environment.NewLine}{{0}}", string.Join("\n", errors));

        return Task.FromResult(this as IAssertAcknowledgementDocument);
    }

    public IAssertAcknowledgementDocument HasOriginalTransactionId(TransactionId originalTransactionId)
    {
        FirstSeriesElement()
            .GetProperty("mRID")
            .GetString()
            .Should()
            .Be(originalTransactionId.Value);
        return this;
    }

    private JsonElement FirstSeriesElement()
    {
        return _root.GetProperty("Series").EnumerateArray().ToList()[0];
    }
}
