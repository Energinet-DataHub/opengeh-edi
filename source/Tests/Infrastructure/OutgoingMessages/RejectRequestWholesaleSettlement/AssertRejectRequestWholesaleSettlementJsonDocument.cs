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

using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Schemas.Cim.Json;
using FluentAssertions;
using Json.Schema;
using NodaTime;
using Xunit;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.RejectRequestWholesaleSettlement;

public sealed class AssertRejectRequestWholesaleSettlementJsonDocument : IAssertRejectRequestWholesaleSettlementDocument
{
    private readonly JsonSchemaProvider _schemas = new(new CimJsonSchemas());
    private readonly JsonDocument _document;
    private readonly JsonElement _root;

    public AssertRejectRequestWholesaleSettlementJsonDocument(Stream document)
    {
        _document = JsonDocument.Parse(document);
        _root = _document.RootElement.GetProperty("RejectRequestWholesaleSettlement_MarketDocument");
    }

    public IAssertRejectRequestWholesaleSettlementDocument HasMessageId(string expectedMessageId)
    {
        _root.GetProperty("mRID").GetString().Should().Be(expectedMessageId);
        return this;
    }

    public IAssertRejectRequestWholesaleSettlementDocument MessageIdExists()
    {
        _root.TryGetProperty("mRID", out _).Should().BeTrue();
        return this;
    }

    public IAssertRejectRequestWholesaleSettlementDocument HasSenderId(string expectedSenderId)
    {
        _root.GetProperty("sender_MarketParticipant.mRID")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(expectedSenderId);
        return this;
    }

    public IAssertRejectRequestWholesaleSettlementDocument HasSenderRole(ActorRole role)
    {
        ArgumentNullException.ThrowIfNull(role);

        _root.GetProperty("sender_MarketParticipant.marketRole.type")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(role.Code);
        return this;
    }

    public IAssertRejectRequestWholesaleSettlementDocument HasReceiverId(string expectedReceiverId)
    {
        _root.GetProperty("receiver_MarketParticipant.mRID")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(expectedReceiverId);
        return this;
    }

    public IAssertRejectRequestWholesaleSettlementDocument HasReceiverRole(ActorRole role)
    {
        ArgumentNullException.ThrowIfNull(role);

        _root.GetProperty("receiver_MarketParticipant.marketRole.type")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(role.Code);
        return this;
    }

    public IAssertRejectRequestWholesaleSettlementDocument HasTimestamp(Instant expectedTimestamp)
    {
        _root.GetProperty("createdDateTime")
            .GetString()
            .Should()
            .Be(expectedTimestamp.ToString());
        return this;
    }

    public IAssertRejectRequestWholesaleSettlementDocument HasReasonCode(string reasonCode)
    {
        _root.GetProperty("reason.code")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(reasonCode);
        return this;
    }

    public async Task<IAssertRejectRequestWholesaleSettlementDocument> DocumentIsValidAsync()
    {
        var schema = await _schemas
            .GetSchemaAsync<JsonSchema>("RejectRequestWholesaleSettlement", "0", CancellationToken.None)
            .ConfigureAwait(false);

        schema.Should().NotBeNull("Cannot validate document without a schema");

        var validationOptions = new EvaluationOptions { OutputFormat = OutputFormat.List };
        var validationResult = schema!.Evaluate(_document, validationOptions);
        var errors = validationResult.Details.Where(detail => detail.HasErrors)
            .Select(x => (x.InstanceLocation, x.EvaluationPath, x.Errors))
            .SelectMany(
                p => p.Errors!.Values.Select(
                    e => $"==> '{p.InstanceLocation}' does not adhere to '{p.EvaluationPath}' with error: {e}\n"));

        validationResult.IsValid.Should().BeTrue($"because document should be valid. Validation errors:{Environment.NewLine}{{0}}", string.Join("\n", errors));

        return this;
    }

    public IAssertRejectRequestWholesaleSettlementDocument HasBusinessReason(BusinessReason businessReason)
    {
        ArgumentNullException.ThrowIfNull(businessReason);

        _root.GetProperty("process.processType")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(businessReason.Code);
        return this;
    }

    public IAssertRejectRequestWholesaleSettlementDocument HasTransactionId(TransactionId expectedTransactionId)
    {
        ArgumentNullException.ThrowIfNull(expectedTransactionId);

        FirstSeriesElement()
            .GetProperty("mRID")
            .GetString()
            .Should()
            .Be(expectedTransactionId.Value);
        return this;
    }

    public IAssertRejectRequestWholesaleSettlementDocument TransactionIdExists()
    {
        FirstSeriesElement()
            .TryGetProperty("mRID", out _)
            .Should()
            .BeTrue();
        return this;
    }

    public IAssertRejectRequestWholesaleSettlementDocument HasSerieReasonCode(string expectedSerieReasonCode)
    {
        FirstReasonElement()
            .GetProperty("code")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(expectedSerieReasonCode);
        return this;
    }

    public IAssertRejectRequestWholesaleSettlementDocument HasSerieReasonMessage(string expectedSerieReasonMessage)
    {
        FirstReasonElement()
            .GetProperty("text")
            .GetString()
            .Should()
            .Be(expectedSerieReasonMessage);
        return this;
    }

    public IAssertRejectRequestWholesaleSettlementDocument HasOriginalTransactionId(
        TransactionId expectedOriginalTransactionId)
    {
        ArgumentNullException.ThrowIfNull(expectedOriginalTransactionId);

        FirstSeriesElement()
            .GetProperty("originalTransactionIDReference_Series.mRID")
            .GetString()
            .Should()
            .Be(expectedOriginalTransactionId.Value);
        return this;
    }

    private JsonElement FirstSeriesElement()
    {
        return _root.GetProperty("Series").EnumerateArray().ToList()[0];
    }

    private JsonElement FirstReasonElement()
    {
        return _root.GetProperty("Series").EnumerateArray().ToList()[0]
            .GetProperty("Reason")
            .EnumerateArray()
            .ToList()[0];
    }
}
