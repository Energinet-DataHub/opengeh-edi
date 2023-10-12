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
using System.Xml;
using Dapper;
using Energinet.DataHub.EDI.Application.Configuration.DataAccess;
using Energinet.DataHub.EDI.Application.OutgoingMessages;
using Energinet.DataHub.EDI.Domain.Actors;
using Energinet.DataHub.EDI.Domain.OutgoingMessages;
using Energinet.DataHub.EDI.Domain.OutgoingMessages.NotifyAggregatedMeasureData;
using Energinet.DataHub.EDI.Domain.Transactions;
using Energinet.DataHub.EDI.Domain.Transactions.Aggregations;
using Energinet.DataHub.EDI.Infrastructure.DocumentValidation;

using Energinet.DataHub.EDI.Infrastructure.DocumentValidation.CimXml;
using Energinet.DataHub.EDI.Infrastructure.OutgoingMessages.Queueing;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Json.Schema;
using Microsoft.VisualStudio.Threading;
using NodaTime.Extensions;
using Xunit;
using Point = Energinet.DataHub.EDI.Domain.Transactions.Aggregations.Point;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.OutgoingMessages;

public class WhenEnqueueingTests : TestBase
{
    private readonly MessageEnqueuer _messageEnqueuer;
    private readonly IOutgoingMessageRepository _outgoingMessageRepository;
    private readonly IOutgoingMessagesConfigurationRepository _outgoingMessagesConfigurationRepository;
    private readonly Energinet.DataHub.EDI.Application.OutgoingMessages.DocumentFactory _documentFactory;

    public WhenEnqueueingTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _messageEnqueuer = GetService<MessageEnqueuer>();
        _outgoingMessageRepository = GetService<IOutgoingMessageRepository>();
        _outgoingMessagesConfigurationRepository = GetService<IOutgoingMessagesConfigurationRepository>();
        _documentFactory = GetService<EDI.Application.OutgoingMessages.DocumentFactory>();
    }

    [Fact]
    public async Task Outgoing_message_is_enqueued()
    {
        var message = CreateOutgoingMessage(_outgoingMessagesConfigurationRepository, _documentFactory);
        await EnqueueMessage(message);
        // TODO: (LRN) Ensure we have a ActorQueue with a bundle with the expected OutgoingMessage.
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var sql = "SELECT * FROM [dbo].[OutgoingMessages]";
        var result = await
            connection
                .QuerySingleOrDefaultAsync(sql);
        Assert.NotNull(result);
        Assert.Equal(result.DocumentType, message.DocumentType.Name);
        Assert.Equal(result.ReceiverId, message.ReceiverId.Value);
        Assert.Equal(result.ReceiverRole, message.ReceiverRole.Name);
        Assert.Equal(result.SenderId, message.SenderId.Value);
        Assert.Equal(result.SenderRole, message.SenderRole.Name);
        Assert.Equal(result.BusinessReason, message.BusinessReason);
        Assert.NotNull(result.MessageRecord);
        Assert.NotNull(result.AssignedBundleId);
    }

    [Fact]
    public async Task Can_peek_message()
    {
        var message = CreateOutgoingMessage(_outgoingMessagesConfigurationRepository, _documentFactory);
        await EnqueueMessage(message);
        var command = new PeekCommand(message.ReceiverId, message.DocumentType.Category, message.ReceiverRole, Domain.Documents.DocumentFormat.Xml);

        var result = await InvokeCommandAsync(command);

        Assert.NotNull(result.MessageId);
    }

    [Fact]
    public async Task Can_peek_message_in_CIM_XML()
    {
        var message = CreateOutgoingMessage(_outgoingMessagesConfigurationRepository, _documentFactory);
        await EnqueueMessage(message);
        var command = new PeekCommand(message.ReceiverId, message.DocumentType.Category, message.ReceiverRole, Domain.Documents.DocumentFormat.Xml);

        var result = await InvokeCommandAsync(command);

        Assert.NotNull(result.Bundle);
        if (result.Bundle is not null)
        {
            var validator = new CimXmlValidator(new CimXmlSchemaProvider());
            var resultValidation = await validator.ValidateAsync(result.Bundle, EDI.Infrastructure.DocumentValidation.DocumentType.AggregationResult, "0.1", CancellationToken.None);
            Assert.True(resultValidation.IsValid);
        }
    }

    [Fact]
    public async Task Can_peek_multiple_messages_in_CIM_XML()
    {
        var message = CreateOutgoingMessage(_outgoingMessagesConfigurationRepository, _documentFactory);
        await EnqueueMessage(message);
        message = CreateOutgoingMessage(_outgoingMessagesConfigurationRepository, _documentFactory);
        await EnqueueMessage(message);
        message = CreateOutgoingMessage(_outgoingMessagesConfigurationRepository, _documentFactory);
        await EnqueueMessage(message);

        var command = new PeekCommand(message.ReceiverId, message.DocumentType.Category, message.ReceiverRole, Domain.Documents.DocumentFormat.Xml);

        var result = await InvokeCommandAsync(command);

        Assert.NotNull(result.Bundle);
        if (result.Bundle is not null)
        {
            var validator = new CimXmlValidator(new CimXmlSchemaProvider());
            var resultValidation = await validator.ValidateAsync(result.Bundle, EDI.Infrastructure.DocumentValidation.DocumentType.AggregationResult, "0.1", CancellationToken.None);
            Assert.True(resultValidation.IsValid);
        }
    }

    [Fact]
    public async Task Can_peek_message_in_JSON_format()
    {
        JsonSchemaProvider schemas = new(new CimJsonSchemas());
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var sql = "insert into OutgoingMessagesConfiguration values('1234567891912','MeteringDataAdministrator','NotifyAggregatedMeasureData','json')";
        _ = await connection.ExecuteAsync(sql);

        var message = CreateOutgoingMessage(_outgoingMessagesConfigurationRepository, _documentFactory);
        await EnqueueMessage(message);
        var command = new PeekCommand(message.ReceiverId, message.DocumentType.Category, message.ReceiverRole, Domain.Documents.DocumentFormat.Json);

        var result = await InvokeCommandAsync(command);

        Assert.NotNull(result.Bundle);
        if (result.Bundle is not null)
        {
            var document = await JsonDocument.ParseAsync(result.Bundle);
            var schema = await schemas.GetSchemaAsync<JsonSchema>("NOTIFYAGGREGATEDMEASUREDATA", "0", CancellationToken.None);
            var validationOptions = new EvaluationOptions()
            {
                OutputFormat = OutputFormat.List,
            };
            var validationResult = schema!.Evaluate(document, validationOptions);
            var errors = validationResult.Details.Where(detail => detail.HasErrors).Select(x => x.Errors).ToList();
            Assert.True(validationResult.IsValid, string.Join("\n", errors));
        }
    }

    [Fact]
    public async Task Can_peek_multiple_messages_in_JSON_format()
    {
        JsonSchemaProvider schemas = new(new CimJsonSchemas());
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var sql = "insert into OutgoingMessagesConfiguration values('1234567891912','MeteringDataAdministrator','NotifyAggregatedMeasureData','json')";
        _ = await connection.ExecuteAsync(sql);

        var message = CreateOutgoingMessage(_outgoingMessagesConfigurationRepository, _documentFactory);
        await EnqueueMessage(message);
        message = CreateOutgoingMessage(_outgoingMessagesConfigurationRepository, _documentFactory);
        await EnqueueMessage(message);
        message = CreateOutgoingMessage(_outgoingMessagesConfigurationRepository, _documentFactory);
        await EnqueueMessage(message);
        var command = new PeekCommand(message.ReceiverId, message.DocumentType.Category, message.ReceiverRole, Domain.Documents.DocumentFormat.Json);

        var result = await InvokeCommandAsync(command);

        Assert.NotNull(result.Bundle);
        if (result.Bundle is not null)
        {
            using var sr = new StreamReader(result.Bundle);
            var text = await sr.ReadToEndAsync();

            result.Bundle.Position = 0;
            var document = await JsonDocument.ParseAsync(result.Bundle);
            var schema = await schemas.GetSchemaAsync<JsonSchema>("NOTIFYAGGREGATEDMEASUREDATA", "0", CancellationToken.None);
            var validationOptions = new EvaluationOptions()
            {
                OutputFormat = OutputFormat.List,
            };
            var validationResult = schema!.Evaluate(document, validationOptions);
            var errors = validationResult.Details.Where(detail => detail.HasErrors).Select(x => x.Errors).ToList();
            Assert.True(validationResult.IsValid, string.Join("\n", errors));
        }
    }

    [Fact]
    public async Task Can_peek_message_in_Ebix()
    {
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var sql = "insert into OutgoingMessagesConfiguration values('1234567891912','MeteringDataAdministrator','NotifyAggregatedMeasureData','ebix')";
        _ = await connection.ExecuteAsync(sql);
        var message = CreateOutgoingMessage(_outgoingMessagesConfigurationRepository, _documentFactory);
        await EnqueueMessage(message);
        var command = new PeekCommand(message.ReceiverId, message.DocumentType.Category, message.ReceiverRole, Domain.Documents.DocumentFormat.Ebix);

        var result = await InvokeCommandAsync(command);

        Assert.NotNull(result.Bundle);
        if (result.Bundle is not null)
        {
            // Simple test to see if we have a valid xmldocument
            var xml = new XmlDocument();
            xml.Load(result.Bundle);

            //var validator = new CimXmlValidator(new CimXmlSchemaProvider());
            //var resultValidation = await validator.ValidateAsync(result.Bundle, EDI.Infrastructure.DocumentValidation.DocumentType.AggregationResult, "0.1", CancellationToken.None).ConfigureAwait(false);
            //Assert.True(resultValidation.IsValid);
        }
    }

    [Fact]
    public async Task Can_dequeue_bundle()
    {
        var message = CreateOutgoingMessage(_outgoingMessagesConfigurationRepository, _documentFactory);
        await EnqueueMessage(message);
        var peekCommand = new PeekCommand(message.ReceiverId, message.DocumentType.Category, message.ReceiverRole, Domain.Documents.DocumentFormat.Xml);
        var peekResult = await InvokeCommandAsync(peekCommand);
        var dequeueCommand = new DequeueCommand(peekResult.MessageId!.Value.ToString(), message.ReceiverRole, message.ReceiverId);

        var result = await InvokeCommandAsync(dequeueCommand);

        Assert.True(result.Success);
    }

    private static OutgoingMessage CreateOutgoingMessage(IOutgoingMessagesConfigurationRepository outgoingMessagesConfigurationRepository, EDI.Application.OutgoingMessages.DocumentFactory documentFactory)
    {
        var documentFormat = Domain.Documents.DocumentFormat.Xml;
        using var context = new JoinableTaskContext();
        var joinableTaskFactory = new JoinableTaskFactory(context);
        _ = joinableTaskFactory.Run(async () => documentFormat = await outgoingMessagesConfigurationRepository.GetDocumentFormatAsync(ActorNumber.Create("1234567891912"), MarketRole.MeteringDataAdministrator, Domain.Documents.DocumentType.NotifyAggregatedMeasureData).ConfigureAwait(false));

        var documentWriter = documentFactory.GetWriter(Domain.Documents.DocumentType.NotifyAggregatedMeasureData, documentFormat);

        var p = new Point(1, 1m, Quality.Calculated.Name, "2022-12-12T23:00:00Z"); //TODO tilføj point
        var points = Array.Empty<Point>();
        var message = AggregationResultMessage.Create(
            ActorNumber.Create("1234567891912"),
            MarketRole.MeteringDataAdministrator,
            ProcessId.Create(Guid.NewGuid()),
            new Aggregation(
                points,
                MeteringPointType.Consumption.Name,
                MeasurementUnit.Kwh.Name,
                Resolution.Hourly.Name,
                new Period(DateTimeOffset.UtcNow.ToInstant(), DateTimeOffset.UtcNow.AddHours(1).ToInstant()),
                SettlementType.NonProfiled.Name,
                BusinessReason.BalanceFixing.Name,
                new ActorGrouping("1234567891911", null),
                new GridAreaDetails("805", "1234567891045"),
                SettlementVersion: "D01",
                OriginalTransactionIdReference: "{E7E9AB49-5A8A-4C81-9FB4-A29CEA6F57B7}"),
            documentWriter);
        return message;
    }

    private async Task EnqueueMessage(OutgoingMessage message)
    {
        _outgoingMessageRepository.Add(message);
        await _messageEnqueuer.EnqueueAsync(message);
        var unitOfWork = GetService<IUnitOfWork>();
        await unitOfWork.CommitAsync();
    }
}
