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

using Dapper;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.IncomingMessages.Application.UseCases;
using Energinet.DataHub.EDI.IncomingMessages.Domain;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Validation.ValidationErrors;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParsers;
using Energinet.DataHub.EDI.IncomingMessages.IntegrationTests.Builders;
using Energinet.DataHub.EDI.IncomingMessages.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;
using Energinet.DataHub.EDI.Process.Interfaces;
using FluentAssertions;
using NodaTime;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.IncomingMessages.IntegrationTests.IncomingMessages;

public class GivenIncomingMeteredDataForMeasurementMessageTests : IncomingMessagesTestBase
{
    private readonly MarketMessageParser _marketMessageParser;
    private readonly IDictionary<(IncomingDocumentType, DocumentFormat), IMessageParser> _messageParsers;
    private readonly ValidateIncomingMessage _validateIncomingMessage;
    private readonly ActorIdentity _actorIdentity;

    public GivenIncomingMeteredDataForMeasurementMessageTests(
        IncomingMessagesTestFixture incomingMessagesTestFixture,
        ITestOutputHelper testOutputHelper)
        : base(incomingMessagesTestFixture, testOutputHelper)
    {
        _marketMessageParser = GetService<MarketMessageParser>();
        _messageParsers = GetService<IList<IMessageParser>>().ToDictionary(
            parser => (parser.DocumentType, parser.DocumentFormat),
            parser => parser);

        var authenticatedActor = GetService<AuthenticatedActor>();
        _actorIdentity = new ActorIdentity(ActorNumber.Create("1234567890123"), restriction: Restriction.None,  ActorRole.FromCode("DDM"));
        authenticatedActor.SetAuthenticatedActor(_actorIdentity);

        _validateIncomingMessage = GetService<ValidateIncomingMessage>();
    }

    [Fact]
    public async Task When_ReceiverIdIsDatahub_Then_ValidationSucceed()
    {
        var validDataHubReceiverId = "5790001330552";
        var documentFormat = DocumentFormat.Ebix;
        var message = MeteredDataForMeasurementPointBuilder.CreateIncomingMessage(
            documentFormat,
            _actorIdentity.ActorNumber,
            [
                ("123456",
                    Instant.FromUtc(2024, 1, 1, 0, 0),
                    Instant.FromUtc(2024, 1, 2, 0, 0),
                    Resolution.QuarterHourly),
            ],
            receiverNumber: validDataHubReceiverId);

        var (incomingMessage, _) = await ParseMessageAsync(message.Stream, documentFormat);
        var result = await _validateIncomingMessage.ValidateAsync(
            incomingMessage!,
            documentFormat,
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task When_ReceiverIdIsNotDatahub_Then_ResultContainExceptedValidationError()
    {
        var invalidDataHubReceiverId = "5790001330052";
        var documentFormat = DocumentFormat.Ebix;
        var message = MeteredDataForMeasurementPointBuilder.CreateIncomingMessage(
            documentFormat,
            _actorIdentity.ActorNumber,
            [
                ("123456",
                    Instant.FromUtc(2024, 1, 1, 0, 0),
                    Instant.FromUtc(2024, 1, 2, 0, 0),
                    Resolution.QuarterHourly),
            ],
            receiverNumber: invalidDataHubReceiverId);

        var (incomingMessage, _) = await ParseMessageAsync(message.Stream, documentFormat);
        var result = await _validateIncomingMessage.ValidateAsync(
            incomingMessage!,
            documentFormat,
            CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(error => error is InvalidReceiverId);
    }

    [Fact]
    public async Task When_SenderIdDoesNotMatchTheAuthenticatedUser_Then_ResultContainExceptedValidationError()
    {
        var documentFormat = DocumentFormat.Ebix;
        var invalidSenderId = ActorNumber.Create("5790001330550");
        var message = MeteredDataForMeasurementPointBuilder.CreateIncomingMessage(
            documentFormat,
            invalidSenderId,
            [
                ("123456",
                    Instant.FromUtc(2024, 1, 1, 0, 0),
                    Instant.FromUtc(2024, 1, 2, 0, 0),
                    Resolution.QuarterHourly),
            ]);

        var (incomingMessage, _) = await ParseMessageAsync(message.Stream, documentFormat);
        var result = await _validateIncomingMessage.ValidateAsync(
            incomingMessage!,
            documentFormat,
            CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(error => error is AuthenticatedUserDoesNotMatchSenderId);
    }

    [Fact]
    public async Task When_MultipleTransactionsWithSameId_Then_ResultContainExceptedValidationError()
    {
        var documentFormat = DocumentFormat.Ebix;
        var duplicatedTransactionId = "123456";
        var message = MeteredDataForMeasurementPointBuilder.CreateIncomingMessage(
            documentFormat,
            _actorIdentity.ActorNumber,
            [
                (duplicatedTransactionId,
                    Instant.FromUtc(2024, 1, 1, 0, 0),
                    Instant.FromUtc(2024, 1, 2, 0, 0),
                    Resolution.QuarterHourly),
                (duplicatedTransactionId,
                    Instant.FromUtc(2024, 1, 1, 0, 0),
                    Instant.FromUtc(2024, 1, 2, 0, 0),
                    Resolution.QuarterHourly),
            ]);

        var (incomingMessage, _) = await ParseMessageAsync(message.Stream, documentFormat);
        var result = await _validateIncomingMessage.ValidateAsync(
            incomingMessage!,
            documentFormat,
            CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(error => error is DuplicateTransactionIdDetected);
    }

    [Fact]
    public async Task When_TransactionIdIsEmpty_Then_ResultContainExceptedValidationError()
    {
        var documentFormat = DocumentFormat.Ebix;
        var emptyTransactionId = string.Empty;
        var message = MeteredDataForMeasurementPointBuilder.CreateIncomingMessage(
            documentFormat,
            _actorIdentity.ActorNumber,
            [
                (emptyTransactionId,
                    Instant.FromUtc(2024, 1, 1, 0, 0),
                    Instant.FromUtc(2024, 1, 2, 0, 0),
                    Resolution.QuarterHourly),
            ]);

        var (incomingMessage, _) = await ParseMessageAsync(message.Stream, documentFormat);
        var result = await _validateIncomingMessage.ValidateAsync(
            incomingMessage!,
            documentFormat,
            CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(error => error is EmptyTransactionId);
    }

    [Fact]
    public async Task When_MessageIdIsEmpty_Then_ResultContainExceptedValidationError()
    {
        var documentFormat = DocumentFormat.Ebix;
        var emptyMessageId = string.Empty;
        var message = MeteredDataForMeasurementPointBuilder.CreateIncomingMessage(
            documentFormat,
            _actorIdentity.ActorNumber,
            [
                ("123456789",
                    Instant.FromUtc(2024, 1, 1, 0, 0),
                    Instant.FromUtc(2024, 1, 2, 0, 0),
                    Resolution.QuarterHourly),
            ],
            messageId: emptyMessageId);

        var (incomingMessage, _) = await ParseMessageAsync(message.Stream, documentFormat);
        var result = await _validateIncomingMessage.ValidateAsync(
            incomingMessage!,
            documentFormat,
            CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(error => error is EmptyMessageId);
    }

    [Fact]
    public async Task When_MessageIdAlreadyExists_Then_ResultContainExceptedValidationError()
    {
        var documentFormat = DocumentFormat.Ebix;
        var existingMessageId = "123564789";
        var message = MeteredDataForMeasurementPointBuilder.CreateIncomingMessage(
            documentFormat,
            _actorIdentity.ActorNumber,
            [
                ("555555555", Instant.FromUtc(2024, 1, 1, 0, 0), Instant.FromUtc(2024, 1, 31, 0, 0), Resolution.QuarterHourly),
            ],
            messageId: existingMessageId);

        await StoreMessageIdForActorAsync(existingMessageId, _actorIdentity.ActorNumber.Value);

        var (incomingMessage, _) = await ParseMessageAsync(message.Stream, documentFormat);
        var result = await _validateIncomingMessage.ValidateAsync(
            incomingMessage!,
            documentFormat,
            CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(error => error is DuplicateMessageIdDetected);
    }

    [Fact]
    public async Task When_SenderRoleInMessageIsMeteredDataResponsible_Then_ValidationSucceed()
    {
        var documentFormat = DocumentFormat.Ebix;
        var validSenderRoleInMessage = ActorRole.MeteredDataResponsible;
        var message = MeteredDataForMeasurementPointBuilder.CreateIncomingMessage(
            documentFormat,
            _actorIdentity.ActorNumber,
            [
                ("555555555", Instant.FromUtc(2024, 1, 1, 0, 0), Instant.FromUtc(2024, 1, 31, 0, 0), Resolution.QuarterHourly),
            ],
            senderRole: validSenderRoleInMessage.Code);

        var (incomingMessage, _) = await ParseMessageAsync(message.Stream, documentFormat);
        var result = await _validateIncomingMessage.ValidateAsync(
            incomingMessage!,
            documentFormat,
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task When_AuthenticatedSenderRoleIsIncorrect_Then_ResultContainExceptedValidationError()
    {
        var documentFormat = DocumentFormat.Ebix;
        var authenticatedActor = GetService<AuthenticatedActor>();
        var invalidSenderRole = ActorRole.EnergySupplier;
        var actorIdentity = new ActorIdentity(ActorNumber.Create("1234567890123"), restriction: Restriction.None,  invalidSenderRole);
        authenticatedActor.SetAuthenticatedActor(actorIdentity);
        var message = MeteredDataForMeasurementPointBuilder.CreateIncomingMessage(
            documentFormat,
            _actorIdentity.ActorNumber,
            [
                ("555555555", Instant.FromUtc(2024, 1, 1, 0, 0), Instant.FromUtc(2024, 1, 31, 0, 0), Resolution.QuarterHourly),
            ]);

        var (incomingMessage, _) = await ParseMessageAsync(message.Stream, documentFormat);
        var result = await _validateIncomingMessage.ValidateAsync(
            incomingMessage!,
            documentFormat,
            CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(error => error is AuthenticatedUserDoesNotHoldRequiredRoleType);
    }

    [Fact]
    public async Task When_BusinessProcessIsIncorrect_Then_ResultContainExceptedValidationError()
    {
        var invalidBusinessProcess = "un:unece:260:data:EEM-DK_DataTimeSeries:v3";
        var documentFormat = DocumentFormat.Ebix;
        var message = MeteredDataForMeasurementPointBuilder.CreateIncomingMessage(
            documentFormat,
            _actorIdentity.ActorNumber,
            [
                ("555555555", Instant.FromUtc(2024, 1, 1, 0, 0), Instant.FromUtc(2024, 1, 31, 0, 0), Resolution.QuarterHourly),
            ],
            schema: invalidBusinessProcess);

        var (_, result) = await ParseMessageAsync(message.Stream, documentFormat);

        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(error => error is InvalidBusinessReasonOrVersion);
    }

    [Fact]
    public async Task When_SchemaIsIncorrect_Then_ResultContainExceptedValidationError()
    {
        var invalidSchema = "EEM-DK_MeteredDataTimeSeries:v3";
        var documentFormat = DocumentFormat.Ebix;
        var message = MeteredDataForMeasurementPointBuilder.CreateIncomingMessage(
            documentFormat,
            _actorIdentity.ActorNumber,
            [
                ("555555555", Instant.FromUtc(2024, 1, 1, 0, 0), Instant.FromUtc(2024, 1, 31, 0, 0), Resolution.QuarterHourly),
            ],
            schema: invalidSchema);

        var (_, result) = await ParseMessageAsync(message.Stream, documentFormat);

        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(error => error is InvalidMessageStructure);
    }

    [Fact]
    public async Task When_ProcessTypeIsNotAllowed_Then_ResultContainExceptedValidationError()
    {
        var notAllowedProcessType = "1880";
        var documentFormat = DocumentFormat.Ebix;
        var message = MeteredDataForMeasurementPointBuilder.CreateIncomingMessage(
            documentFormat,
            _actorIdentity.ActorNumber,
            [
                ("555555555", Instant.FromUtc(2024, 1, 1, 0, 0), Instant.FromUtc(2024, 1, 31, 0, 0), Resolution.QuarterHourly),
            ],
            processType: notAllowedProcessType);

        var (_, result) = await ParseMessageAsync(message.Stream, documentFormat);

        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(error => error is InvalidMessageStructure);
    }

    [Theory]
    [InlineData("E23")]
    [InlineData("D42")]
    public async Task When_ProcessTypeIsAllowedForEbix_Then_ValidationSucceed(string allowedProcessType)
    {
        var documentFormat = DocumentFormat.Ebix;
        var message = MeteredDataForMeasurementPointBuilder.CreateIncomingMessage(
            documentFormat,
            _actorIdentity.ActorNumber,
            [
                ("555555555", Instant.FromUtc(2024, 1, 1, 0, 0), Instant.FromUtc(2024, 1, 31, 0, 0), Resolution.QuarterHourly),
            ],
            processType: allowedProcessType);

        var (incomingMessage, _) = await ParseMessageAsync(message.Stream, documentFormat);
        var result = await _validateIncomingMessage.ValidateAsync(
            incomingMessage!,
            documentFormat,
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task When_ProcessTypeIsAllowedForCim_Then_ValidationSucceed()
    {
        var allowedProcessType = "E23";
        var documentFormat = DocumentFormat.Xml;
        var message = MeteredDataForMeasurementPointBuilder.CreateIncomingMessage(
            documentFormat,
            _actorIdentity.ActorNumber,
            [
                ("555555555", Instant.FromUtc(2024, 1, 1, 0, 0), Instant.FromUtc(2024, 1, 31, 0, 0), Resolution.QuarterHourly),
            ],
            processType: allowedProcessType);

        var (incomingMessage, _) = await ParseMessageAsync(message.Stream, documentFormat);
        var result = await _validateIncomingMessage.ValidateAsync(
            incomingMessage!,
            documentFormat,
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task When_ProcessTypeIsNotAllowedForCim_Then_ResultContainExceptedValidationError()
    {
        var notAllowedProcessType = "D42";
        var documentFormat = DocumentFormat.Xml;
        var message = MeteredDataForMeasurementPointBuilder.CreateIncomingMessage(
            documentFormat,
            _actorIdentity.ActorNumber,
            [
                ("555555555", Instant.FromUtc(2024, 1, 1, 0, 0), Instant.FromUtc(2024, 1, 31, 0, 0), Resolution.QuarterHourly),
            ],
            processType: notAllowedProcessType);

        var (incomingMessage, _) = await ParseMessageAsync(message.Stream, documentFormat);
        var result = await _validateIncomingMessage.ValidateAsync(
            incomingMessage!,
            documentFormat,
            CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(error => error is NotSupportedProcessType);
    }

    [Fact]
    public async Task When_MessageTypeIsNotAllowed_Then_ResultContainExceptedValidationError()
    {
        var notAllowedMessageType = "1880";
        var documentFormat = DocumentFormat.Ebix;
        var message = MeteredDataForMeasurementPointBuilder.CreateIncomingMessage(
            documentFormat,
            _actorIdentity.ActorNumber,
            [
                ("555555555", Instant.FromUtc(2024, 1, 1, 0, 0), Instant.FromUtc(2024, 1, 31, 0, 0), Resolution.QuarterHourly),
            ],
            messageType: notAllowedMessageType);

        var (_, result) = await ParseMessageAsync(message.Stream, documentFormat);

        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(error => error is InvalidMessageStructure);
        result.Errors.Should().Contain(error => error.Message.Contains(
            new NotSupportedMessageType(string.Empty).Target!,
            StringComparison.InvariantCultureIgnoreCase));
    }

    [Fact]
    public async Task When_MessageTypeIsAllowed_Then_ResultContainExceptedValidationError()
    {
        var allowedMessageType = "E66";
        var documentFormat = DocumentFormat.Ebix;
        var message = MeteredDataForMeasurementPointBuilder.CreateIncomingMessage(
            documentFormat,
            _actorIdentity.ActorNumber,
            [
                ("555555555", Instant.FromUtc(2024, 1, 1, 0, 0), Instant.FromUtc(2024, 1, 31, 0, 0), Resolution.QuarterHourly),
            ],
            messageType: allowedMessageType);

        var (_, result) = await ParseMessageAsync(message.Stream, documentFormat);

        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task When_MessageIdIs35Characters_Then_ValidationSucceed()
    {
        var documentFormat = DocumentFormat.Ebix;
        var validMessageId = "12356478912356478912356478912356478";
        var message = MeteredDataForMeasurementPointBuilder.CreateIncomingMessage(
            documentFormat,
            _actorIdentity.ActorNumber,
            [
                ("123456798",
                    Instant.FromUtc(2024, 1, 1, 0, 0), Instant.FromUtc(2024, 1, 2, 0, 0), Resolution.QuarterHourly),
            ],
            messageId: validMessageId);

        var (incomingMessage, _) = await ParseMessageAsync(message.Stream, documentFormat);
        var result = await _validateIncomingMessage.ValidateAsync(
            incomingMessage!,
            documentFormat,
            CancellationToken.None);

        result.Errors.Should().BeEmpty();
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task When_MessageIdExceed35Characters_Then_ExpectedValidationError()
    {
        var invalidMessageId = "123564789123564789123564789123564789_123564789123564789123564789123564789";
        var documentFormat = DocumentFormat.Ebix;
        var message = MeteredDataForMeasurementPointBuilder.CreateIncomingMessage(
            documentFormat,
            _actorIdentity.ActorNumber,
            [
                ("123456789",
                    Instant.FromUtc(2024, 1, 1, 0, 0),
                    Instant.FromUtc(2024, 1, 2, 0, 0),
                    Resolution.QuarterHourly),
            ],
            messageId: invalidMessageId);

        var messageParser = await ParseMessageAsync(message.Stream, documentFormat);

        messageParser.ParserResult.Errors.Should().Contain(error => error is InvalidMessageStructure);
        messageParser.ParserResult.Success.Should().BeFalse();
    }

    [Fact]
    public async Task When_TransactionIdIsLessThen35Characters_Then_ValidationSucceed()
    {
        var documentFormat = DocumentFormat.Ebix;
        var validTransactionId = "1235647891235647891235647891";
        var message = MeteredDataForMeasurementPointBuilder.CreateIncomingMessage(
            documentFormat,
            _actorIdentity.ActorNumber,
            [
                (validTransactionId,
                    Instant.FromUtc(2024, 1, 1, 0, 0), Instant.FromUtc(2024, 1, 2, 0, 0), Resolution.QuarterHourly),
            ]);

        var (incomingMessage, _) = await ParseMessageAsync(message.Stream, documentFormat);

        var result = await _validateIncomingMessage.ValidateAsync(
            incomingMessage!,
            documentFormat,
            CancellationToken.None);

        result.Errors.Should().BeEmpty();
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task When_TransactionIdIs35Characters_Then_ValidationSucceed()
    {
        var documentFormat = DocumentFormat.Ebix;
        var validTransactionId = "12356478912356478912356478912356478";
        var message = MeteredDataForMeasurementPointBuilder.CreateIncomingMessage(
            documentFormat,
            _actorIdentity.ActorNumber,
            [
                (validTransactionId,
                    Instant.FromUtc(2024, 1, 1, 0, 0), Instant.FromUtc(2024, 1, 2, 0, 0), Resolution.QuarterHourly),
            ]);

        var (incomingMessage, _) = await ParseMessageAsync(message.Stream, documentFormat);
        var result = await _validateIncomingMessage.ValidateAsync(
            incomingMessage!,
            documentFormat,
            CancellationToken.None);

        result.Errors.Should().BeEmpty();
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task When_TransactionIdExceed35Characters_Then_ExpectedValidationError()
    {
        var invalidTransactionId = "123564789123564789123564789123564789_123564789123564789123564789123564789";
        var documentFormat = DocumentFormat.Ebix;
        var message = MeteredDataForMeasurementPointBuilder.CreateIncomingMessage(
            documentFormat,
            _actorIdentity.ActorNumber,
            [
                (invalidTransactionId,
                    Instant.FromUtc(2024, 1, 1, 0, 0),
                    Instant.FromUtc(2024, 1, 2, 0, 0),
                    Resolution.QuarterHourly),
            ]);

        var messageParser = await ParseMessageAsync(message.Stream, documentFormat);

        messageParser.ParserResult.Errors.Should().Contain(error => error is InvalidMessageStructure);
        messageParser.ParserResult.Success.Should().BeFalse();
    }

    [Fact]
    public async Task When_BusinessTypeIsAllowed_Then_ValidationSucceed()
    {
        var documentFormat = DocumentFormat.Ebix;
        var message = MeteredDataForMeasurementPointBuilder.CreateIncomingMessage(
            documentFormat,
            _actorIdentity.ActorNumber,
            [
                ("555555555",
                    Instant.FromUtc(2024, 1, 1, 0, 0), Instant.FromUtc(2024, 1, 2, 0, 0), Resolution.QuarterHourly),
            ],
            businessType: "23");

        var messageParser = await ParseMessageAsync(message.Stream, documentFormat);
        var result = await _validateIncomingMessage.ValidateAsync(
            messageParser.IncomingMessage!,
            documentFormat,
            CancellationToken.None);

        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task When_BusinessTypeIsNotAllowed_Then_ExpectedValidationError()
    {
        var documentFormat = DocumentFormat.Ebix;
        var message = MeteredDataForMeasurementPointBuilder.CreateIncomingMessage(
            documentFormat,
            _actorIdentity.ActorNumber,
            [
                ("555555555",
                    Instant.FromUtc(2024, 1, 1, 0, 0), Instant.FromUtc(2024, 1, 2, 0, 0), Resolution.QuarterHourly),
            ],
            businessType: "27");

        var messageParser = await ParseMessageAsync(message.Stream, documentFormat);
        messageParser.ParserResult.Errors.Should().Contain(error => error is InvalidMessageStructure);
        messageParser.ParserResult.Success.Should().BeFalse();
    }

    private async Task<(MeteredDataForMeasurementPointMessage? IncomingMessage, IncomingMarketMessageParserResult ParserResult)> ParseMessageAsync(
        Stream message,
        DocumentFormat documentFormat)
    {
        var incomingMarketMessageStream = new IncomingMarketMessageStream(message);
        if (_messageParsers.TryGetValue((IncomingDocumentType.MeteredDataForMeasurementPoint, documentFormat), out var messageParser))
        {
            var result = await messageParser.ParseAsync(incomingMarketMessageStream, CancellationToken.None).ConfigureAwait(false);
            return (IncomingMessage: (MeteredDataForMeasurementPointMessage?)result.IncomingMessage, ParserResult: result);
        }

        var messageMarketParser = await _marketMessageParser.ParseAsync(
            incomingMarketMessageStream,
            documentFormat,
            IncomingDocumentType.MeteredDataForMeasurementPoint,
            CancellationToken.None);
        return (IncomingMessage: (MeteredDataForMeasurementPointMessage?)messageMarketParser.IncomingMessage, ParserResult: messageMarketParser);
    }

    private async Task StoreMessageIdForActorAsync(string messageId, string senderActorNumber)
    {
        var databaseConnectionFactory = GetService<IDatabaseConnectionFactory>();
        using var dbConnection = await databaseConnectionFactory.GetConnectionAndOpenAsync(CancellationToken.None).ConfigureAwait(false);

        await dbConnection.ExecuteAsync(
                "INSERT INTO [dbo].[MessageRegistry] ([MessageId], [SenderId]) VALUES (@MessageId, @SenderId)",
                new { MessageId = messageId, SenderId = senderActorNumber })
            .ConfigureAwait(false);
    }
}
