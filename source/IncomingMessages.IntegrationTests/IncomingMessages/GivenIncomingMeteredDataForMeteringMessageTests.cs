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

using System.Diagnostics;
using Dapper;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.IncomingMessages.Application.UseCases;
using Energinet.DataHub.EDI.IncomingMessages.Domain.MessageParsers;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Messages;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Validation.ValidationErrors;
using Energinet.DataHub.EDI.IncomingMessages.IntegrationTests.Builders;
using Energinet.DataHub.EDI.IncomingMessages.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;
using FluentAssertions;
using NodaTime;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.IncomingMessages.IntegrationTests.IncomingMessages;

public class GivenIncomingMeteredDataForMeteringMessageTests : IncomingMessagesTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IDictionary<(IncomingDocumentType, DocumentFormat), IMessageParser> _messageParsers;
    private readonly ValidateIncomingMessage _validateIncomingMessage;
    private readonly ActorIdentity _actorIdentity;

    public GivenIncomingMeteredDataForMeteringMessageTests(
        IncomingMessagesTestFixture incomingMessagesTestFixture,
        ITestOutputHelper testOutputHelper)
        : base(incomingMessagesTestFixture, testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _messageParsers = GetService<IEnumerable<IMessageParser>>().ToDictionary(
            parser => (parser.DocumentType, parser.DocumentFormat),
            parser => parser);

        var authenticatedActor = GetService<AuthenticatedActor>();
        _actorIdentity = new ActorIdentity(
            ActorNumber.Create("1234567890123"),
            restriction: Restriction.None,
            ActorRole.FromCode("DDM"),
            null,
            ActorId);
        authenticatedActor.SetAuthenticatedActor(_actorIdentity);

        _validateIncomingMessage = GetService<ValidateIncomingMessage>();
    }

    public static TheoryData<DocumentFormat> GetAllDocumentFormat => new(
        EnumerationType.GetAll<DocumentFormat>().ToArray());

    [Fact]
    public async Task When_ReceiverIdIsDatahub_Then_ValidationSucceed()
    {
        var validDataHubReceiverId = "5790001330552";
        var documentFormat = DocumentFormat.Xml;
        var message = MeteredDataForMeteringPointBuilder.CreateIncomingMessage(
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
        var documentFormat = DocumentFormat.Xml;
        var message = MeteredDataForMeteringPointBuilder.CreateIncomingMessage(
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
        var documentFormat = DocumentFormat.Xml;
        var invalidSenderId = ActorNumber.Create("5790001330550");
        var message = MeteredDataForMeteringPointBuilder.CreateIncomingMessage(
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
        var documentFormat = DocumentFormat.Xml;
        var duplicatedTransactionId = "123456";
        var message = MeteredDataForMeteringPointBuilder.CreateIncomingMessage(
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
    public async Task When_MultipleTransactionsWithSameIdAsExisting_Then_ResultContainExceptedValidationError()
    {
        var documentFormat = DocumentFormat.Xml;
        var existingTransactionIdForSender = "123456";
        var newTransactionIdForSender = "654321";
        await StoreTransactionIdForActorAsync(existingTransactionIdForSender, _actorIdentity.ActorNumber.Value);
        var message = MeteredDataForMeteringPointBuilder.CreateIncomingMessage(
            documentFormat,
            _actorIdentity.ActorNumber,
            [
                (existingTransactionIdForSender,
                    Instant.FromUtc(2024, 1, 1, 0, 0),
                    Instant.FromUtc(2024, 1, 2, 0, 0),
                    Resolution.QuarterHourly),
                (newTransactionIdForSender,
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
        var documentFormat = DocumentFormat.Xml;
        var emptyTransactionId = string.Empty;
        var message = MeteredDataForMeteringPointBuilder.CreateIncomingMessage(
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
        var documentFormat = DocumentFormat.Xml;
        var emptyMessageId = string.Empty;
        var message = MeteredDataForMeteringPointBuilder.CreateIncomingMessage(
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
        var documentFormat = DocumentFormat.Xml;
        var existingMessageId = "123564789";
        var message = MeteredDataForMeteringPointBuilder.CreateIncomingMessage(
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
        var documentFormat = DocumentFormat.Xml;
        var validSenderRoleInMessage = ActorRole.MeteredDataResponsible;
        var message = MeteredDataForMeteringPointBuilder.CreateIncomingMessage(
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
    public async Task AndGiven_SenderRoleIsGridAccessProvider_When_Parsing_Then_HasValidationError()
    {
        var documentFormat = DocumentFormat.Xml;
        var validSenderRoleInMessage = ActorRole.GridAccessProvider;
        var message = MeteredDataForMeteringPointBuilder.CreateIncomingMessage(
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

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle().And.Contain(error => error is SenderRoleTypeIsNotAuthorized);
    }

    [Fact]
    public async Task When_AuthenticatedSenderRoleIsIncorrect_Then_ResultContainExceptedValidationError()
    {
        var documentFormat = DocumentFormat.Xml;
        var authenticatedActor = GetService<AuthenticatedActor>();
        var invalidSenderRole = ActorRole.EnergySupplier;
        var actorIdentity = new ActorIdentity(ActorNumber.Create("1234567890123"), restriction: Restriction.None, invalidSenderRole, null, ActorId);
        authenticatedActor.SetAuthenticatedActor(actorIdentity);
        var message = MeteredDataForMeteringPointBuilder.CreateIncomingMessage(
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
        var invalidBusinessProcess = "urn:ediel.org:measure:notifywholesaleservices:0:2";
        var documentFormat = DocumentFormat.Xml;
        var message = MeteredDataForMeteringPointBuilder.CreateIncomingMessage(
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
        var documentFormat = DocumentFormat.Xml;
        var message = MeteredDataForMeteringPointBuilder.CreateIncomingMessage(
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
        var documentFormat = DocumentFormat.Xml;
        var message = MeteredDataForMeteringPointBuilder.CreateIncomingMessage(
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
        var message = MeteredDataForMeteringPointBuilder.CreateIncomingMessage(
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
        var message = MeteredDataForMeteringPointBuilder.CreateIncomingMessage(
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
        var message = MeteredDataForMeteringPointBuilder.CreateIncomingMessage(
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
        var documentFormat = DocumentFormat.Xml;
        var message = MeteredDataForMeteringPointBuilder.CreateIncomingMessage(
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
        var documentFormat = DocumentFormat.Xml;
        var message = MeteredDataForMeteringPointBuilder.CreateIncomingMessage(
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
        var documentFormat = DocumentFormat.Xml;
        var validMessageId = "12356478912356478912356478912356478";
        var message = MeteredDataForMeteringPointBuilder.CreateIncomingMessage(
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
    public async Task When_MessageIdExceed35Characters_Then_NoValidationError()
    {
        var invalidMessageId = "123564789123564789123564789123564789_123564789123564789123564789123564789";
        var documentFormat = DocumentFormat.Xml;
        var message = MeteredDataForMeteringPointBuilder.CreateIncomingMessage(
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

        messageParser.ParserResult.Errors.Should().BeEmpty();
        messageParser.ParserResult.Success.Should().BeTrue();
    }

    [Fact]
    public async Task When_TransactionIdIsLessThen35Characters_Then_ValidationSucceed()
    {
        var documentFormat = DocumentFormat.Xml;
        var validTransactionId = "1235647891235647891235647891";
        var message = MeteredDataForMeteringPointBuilder.CreateIncomingMessage(
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
        var documentFormat = DocumentFormat.Xml;
        var validTransactionId = "12356478912356478912356478912356478";
        var message = MeteredDataForMeteringPointBuilder.CreateIncomingMessage(
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
    public async Task When_TransactionIdExceed35Characters_Then_NoValidationError()
    {
        var invalidTransactionId = "123564789123564789123564789123564789_123564789123564789123564789123564789";
        var documentFormat = DocumentFormat.Xml;
        var message = MeteredDataForMeteringPointBuilder.CreateIncomingMessage(
            documentFormat,
            _actorIdentity.ActorNumber,
            [
                (invalidTransactionId,
                    Instant.FromUtc(2024, 1, 1, 0, 0),
                    Instant.FromUtc(2024, 1, 2, 0, 0),
                    Resolution.QuarterHourly),
            ]);

        var messageParser = await ParseMessageAsync(message.Stream, documentFormat);

        messageParser.ParserResult.Errors.Should().BeEmpty();
        messageParser.ParserResult.Success.Should().BeTrue();
    }

    [Fact]
    public async Task When_BusinessTypeIsAllowed_Then_ValidationSucceed()
    {
        var documentFormat = DocumentFormat.Xml;
        var message = MeteredDataForMeteringPointBuilder.CreateIncomingMessage(
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
        var documentFormat = DocumentFormat.Xml;
        var message = MeteredDataForMeteringPointBuilder.CreateIncomingMessage(
            documentFormat,
            _actorIdentity.ActorNumber,
            [
                ("555555555",
                    Instant.FromUtc(2024, 1, 1, 0, 0), Instant.FromUtc(2024, 1, 2, 0, 0), Resolution.QuarterHourly),
            ],
            businessType: "42");

        var messageParser = await ParseMessageAsync(message.Stream, documentFormat);
        messageParser.ParserResult.Errors.Should().Contain(error => error is InvalidMessageStructure);
        messageParser.ParserResult.Success.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(GetAllDocumentFormat))]
    public async Task AndGiven_EachTransactionHasOneYearDataWithQuarterlyResolution_When_MessageSizeIs50mb_Then_ValidationSucceedAndWithinAllowedExecutionTimeAndMemoryUsage(DocumentFormat documentFormat)
    {
        // Measure memory before
        var memoryBefore = GC.GetTotalMemory(false);

        // Max number of transactions based on document format before exceeding the limit of 50mb
        var maxNumberOfTransactions = DocumentFormat.Ebix == documentFormat ? 6
                           : DocumentFormat.Xml == documentFormat ? 12
                           : 17;

        // Create transaction for document format
        var transactions = new List<(string TransactionId, Instant PeriodStart, Instant PeriodEnd, Resolution Resolution)>();
        for (var i = 0; i < maxNumberOfTransactions; i++)
        {
           transactions.Add(
               (
                   TransactionId: $"{i}{i}{i}{i}",
                   PeriodStart: Instant.FromUtc(2024, 1, 1, 0, 0),
                   PeriodEnd: Instant.FromUtc(2024, 12, 31, 23, 45),
                   Resolution: Resolution.QuarterHourly));
        }

        var message = MeteredDataForMeteringPointBuilder.CreateIncomingMessage(
            documentFormat,
            _actorIdentity.ActorNumber,
            transactions,
            businessType: "23");

        long messageSizeInBytes = 0;
        if (message.Stream.CanSeek)
        {
            messageSizeInBytes = message.Stream.Length;
            _testOutputHelper.WriteLine($"Message Stream size in bytes: {messageSizeInBytes} ({messageSizeInBytes / 1024 / 1024} MB)");

            // Rewind the stream to the beginning
            message.Stream.Position = 0;
        }

        // Act
        var stopwatch = Stopwatch.StartNew();
        var messageParser = await ParseMessageAsync(message.Stream, documentFormat);
        var result = await _validateIncomingMessage.ValidateAsync(
            messageParser.IncomingMessage!,
            documentFormat,
            CancellationToken.None);

        stopwatch.Stop();

        // Measure memory after
        var memoryAfter = GC.GetTotalMemory(false);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();

        // Currently allowing 25 times the message size in bytes
        var maxAllowedMemoryUse = messageSizeInBytes * 25;
        var memoryUsed = memoryAfter - memoryBefore;

        Assert.True(memoryUsed < maxAllowedMemoryUse, $"Memory used: {memoryUsed} bytes ({memoryUsed / 1024 / 1024} MB), expected {maxAllowedMemoryUse} bytes ({maxAllowedMemoryUse / 1024 / 1024} MB) for {documentFormat}.");
        Assert.True(stopwatch.ElapsedMilliseconds < 60000, $"Execution time: {stopwatch.ElapsedMilliseconds} ms, expected less than 60000ms. for {documentFormat}.");
        _testOutputHelper.WriteLine($"Memory used: {memoryUsed} bytes ({memoryUsed / 1024 / 1024} MB) within {stopwatch.ElapsedMilliseconds}ms for {documentFormat}.");
    }

    private async Task<(MeteredDataForMeteringPointMessageBase? IncomingMessage, IncomingMarketMessageParserResult ParserResult)> ParseMessageAsync(
        Stream message,
        DocumentFormat documentFormat)
    {
        var incomingMarketMessageStream = new IncomingMarketMessageStream(message);
        if (_messageParsers.TryGetValue((IncomingDocumentType.NotifyValidatedMeasureData, documentFormat), out var messageParser))
        {
            var result = await messageParser.ParseAsync(incomingMarketMessageStream, CancellationToken.None).ConfigureAwait(false);
            return (IncomingMessage: (MeteredDataForMeteringPointMessageBase?)result.IncomingMessage, ParserResult: result);
        }

        throw new NotSupportedException($"No message parser found for message format '{documentFormat}' and document type '{IncomingDocumentType.NotifyValidatedMeasureData}'");
    }

    private async Task StoreTransactionIdForActorAsync(string existingTransactionIdForSender, string senderActorNumber)
    {
        var databaseConnectionFactory = GetService<IDatabaseConnectionFactory>();
        using var dbConnection = await databaseConnectionFactory.GetConnectionAndOpenAsync(CancellationToken.None).ConfigureAwait(false);

        await dbConnection.ExecuteAsync(
                "INSERT INTO [dbo].[TransactionRegistry] ([TransactionId], [SenderId]) VALUES (@TransactionId, @SenderId)",
                new { TransactionId = existingTransactionIdForSender, SenderId = senderActorNumber })
            .ConfigureAwait(false);
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
