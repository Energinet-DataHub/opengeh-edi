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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.IncomingMessages.Application.MessageParser;
using Energinet.DataHub.EDI.IncomingMessages.Application.Messages;
using Energinet.DataHub.EDI.IncomingMessages.Application.MessageValidators;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Messages;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Messages.RequestAggregatedMeasureData;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.ValidationErrors;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.MasterData.Interfaces.Models;
using Energinet.DataHub.EDI.Process.Application.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.Process.Interfaces;
using Xunit;
using Serie = Energinet.DataHub.EDI.Process.Interfaces.Serie;

namespace Energinet.DataHub.EDI.IntegrationTests.Infrastructure.CimMessageAdapter.Messages.RequestAggregatedMeasureData;

public class RequestAggregatedMeasureDataReceiverTests : TestBase, IAsyncLifetime
{
    private readonly MarketMessageParser _marketMessageParser;
    private readonly ProcessContext _processContext;
    private readonly RequestAggregatedMeasureDataMessageValidator _requestAggregatedMeasureDataMessageValidator;

    public RequestAggregatedMeasureDataReceiverTests(IntegrationTestFixture integrationTestFixture)
        : base(integrationTestFixture)
    {
        _marketMessageParser = GetService<MarketMessageParser>();
        _processContext = GetService<ProcessContext>();

        var authenticatedActor = GetService<AuthenticatedActor>();
        authenticatedActor.SetAuthenticatedActor(new ActorIdentity(ActorNumber.Create("1234567890123"), restriction: Restriction.None,  ActorRole.FromCode("DDQ")));

        _requestAggregatedMeasureDataMessageValidator = GetService<RequestAggregatedMeasureDataMessageValidator>();
    }

    public static IEnumerable<object[]> AllowedActorRoles =>
        new List<object[]>
        {
            new object[] { ActorRole.EnergySupplier.Code },
            new object[] { ActorRole.MeteredDataResponsible.Code },
            new object[] { ActorRole.BalanceResponsibleParty.Code },
        };

    public async Task InitializeAsync()
    {
#pragma warning disable CA2007

        await CreateActorIfNotExistAsync(
            new CreateActorDto(
                SampleData.StsAssignedUserId,
                ActorNumber.Create(SampleData.SenderId)));

        await CreateActorIfNotExistAsync(
            new CreateActorDto(
                SampleData.SecondStsAssignedUserId,
                ActorNumber.Create(SampleData.SecondSenderId)));
    }

    public Task DisposeAsync()
    {
        _processContext.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Receiver_id_must_be_unknown()
    {
        var unknownReceiverId = "5790001330550";
        var knownReceiverRole = "DGL";
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithReceiverRole(knownReceiverRole)
            .WithReceiverId(unknownReceiverId)
            .Message();

        var messageParserResult = await ParseMessageAsync(message);
        var result = await _requestAggregatedMeasureDataMessageValidator.ValidateAsync((RequestAggregatedMeasureDataMessage)messageParserResult.IncomingMessage!, CancellationToken.None);

        Assert.Contains(result.Errors, error => error is InvalidReceiverId);
    }

    [Fact]
    public async Task Receiver_id_must_be_Datahub()
    {
        var dataHubReceiverId = "5790001330552";
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithReceiverId(dataHubReceiverId)
            .Message();

        var messageParserResult = await ParseMessageAsync(message);
        var result = await _requestAggregatedMeasureDataMessageValidator.ValidateAsync((RequestAggregatedMeasureDataMessage)messageParserResult.IncomingMessage!, CancellationToken.None);

        Assert.DoesNotContain(result.Errors, error => error is InvalidReceiverId);
    }

    [Fact]
    public async Task Receiver_role_must_be_calculation_responsible()
    {
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithReceiverRole("DGL")
            .Message();

        var messageParserResult = await ParseMessageAsync(message);
        var result = await _requestAggregatedMeasureDataMessageValidator.ValidateAsync((RequestAggregatedMeasureDataMessage)messageParserResult.IncomingMessage!, CancellationToken.None);

        Assert.DoesNotContain(result.Errors, error => error is InvalidReceiverRole);
    }

    [Fact]
    public async Task Receiver_role_must_be_known()
    {
        var invalidReceiverRole = "DDD";
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithReceiverRole(invalidReceiverRole)
            .Message();

        var messageParserResult = await ParseMessageAsync(message);

        Assert.False(messageParserResult.Success);
        Assert.Contains(messageParserResult.Errors, error => error is InvalidMessageStructure);
        Assert.Contains(messageParserResult.Errors, error => error.Message.Contains(new InvalidReceiverRole().Target!, StringComparison.InvariantCultureIgnoreCase));
    }

    [Fact]
    public async Task Sender_id_must_match_the_organization_of_the_current_authenticated_user()
    {
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithSenderId(SampleData.SenderId)
            .Message();

        var messageParserResult = await ParseMessageAsync(message);
        var result = await _requestAggregatedMeasureDataMessageValidator.ValidateAsync((RequestAggregatedMeasureDataMessage)messageParserResult.IncomingMessage!, CancellationToken.None);

        Assert.DoesNotContain(result.Errors, error => error is AuthenticatedUserDoesNotMatchSenderId);
    }

    [Fact]
    public async Task Sender_id_does_not_match_the_current_authenticated_user()
    {
        var invalidSenderId = "5790001330550";
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithSenderId(invalidSenderId)
            .Message();

        var messageParserResult = await ParseMessageAsync(message);
        var result = await _requestAggregatedMeasureDataMessageValidator.ValidateAsync((RequestAggregatedMeasureDataMessage)messageParserResult.IncomingMessage!, CancellationToken.None);

        Assert.Contains(result.Errors, error => error is AuthenticatedUserDoesNotMatchSenderId);
    }

    [Fact]
    public async Task Authenticated_user_must_hold_the_role_type_as_specified_in_message()
    {
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithSenderRole("MDR")
            .Message();

        var messageParserResult = await ParseMessageAsync(message);
        var result = await _requestAggregatedMeasureDataMessageValidator.ValidateAsync((RequestAggregatedMeasureDataMessage)messageParserResult.IncomingMessage!, CancellationToken.None);

        Assert.Contains(result.Errors, error => error is AuthenticatedUserDoesNotHoldRequiredRoleType);
    }

    [Fact]
    public async Task Series_must_have_unique_transaction_ids()
    {
        var knownReceiverId = "5790001330552";
        var knownReceiverRole = "DDZ";
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithReceiverRole(knownReceiverRole)
            .WithReceiverId(knownReceiverId)
            .DuplicateSeriesRecords()
            .Message();

        var messageParserResult = await ParseMessageAsync(message);
        var result = await _requestAggregatedMeasureDataMessageValidator.ValidateAsync((RequestAggregatedMeasureDataMessage)messageParserResult.IncomingMessage!, CancellationToken.None);

        Assert.Contains(result.Errors, error => error is DuplicateTransactionIdDetected);
    }

    [Fact]
    public async Task Series_can_have_same_transaction_ids_across_senders()
    {
        var knownReceiverId = "5790001330552";
        var knownReceiverRole = "DDZ";
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithReceiverRole(knownReceiverRole)
            .WithReceiverId(knownReceiverId)
            .Message();

        var messageParserResult = await ParseMessageAsync(message);
        var resultFromFirstMessage = await _requestAggregatedMeasureDataMessageValidator.ValidateAsync((RequestAggregatedMeasureDataMessage)messageParserResult.IncomingMessage!, CancellationToken.None);

        // Request from a second sender.
        await using var message02 = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithReceiverRole(knownReceiverRole)
            .WithReceiverId(knownReceiverId)
            .WithMessageId("123564789123564789123564789123564789")
            .Message();

        var messageParserResult2 = await ParseMessageAsync(message02);
        var resultFromSecondMessage = await _requestAggregatedMeasureDataMessageValidator.ValidateAsync((RequestAggregatedMeasureDataMessage)messageParserResult2.IncomingMessage!, CancellationToken.None);

        Assert.DoesNotContain(resultFromFirstMessage.Errors, error => error is DuplicateTransactionIdDetected);
        Assert.DoesNotContain(resultFromSecondMessage.Errors, error => error is DuplicateTransactionIdDetected);
    }

    [Fact]
    public async Task Series_must_have_none_empty_transaction_ids()
    {
        var knownReceiverId = "5790001330552";
        var knownReceiverRole = "DDZ";
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithReceiverRole(knownReceiverRole)
            .WithReceiverId(knownReceiverId)
            .WithSeriesTransactionId(string.Empty)
            .Message();

        var messageParserResult = await ParseMessageAsync(message);
        var result = await _requestAggregatedMeasureDataMessageValidator.ValidateAsync((RequestAggregatedMeasureDataMessage)messageParserResult.IncomingMessage!, CancellationToken.None);

        Assert.Contains(result.Errors, error => error is EmptyTransactionId);
    }

    [Fact]
    public async Task Message_id_must_not_be_empty()
    {
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithMessageId(string.Empty)
            .Message();

        var messageParserResult = await ParseMessageAsync(message);
        var result = await _requestAggregatedMeasureDataMessageValidator.ValidateAsync((RequestAggregatedMeasureDataMessage)messageParserResult.IncomingMessage!, CancellationToken.None);

        Assert.Contains(result.Errors, error => error is EmptyMessageId);
    }

    [Fact]
    public async Task Message_id_may_be_reused_across_senders()
    {
        await using var message01 = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .Message();
        await using var message02 = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithSenderId("1212121212121")
            .Message();

        var messageParserResult01 = await ParseMessageAsync(message01);
        var result01 = await _requestAggregatedMeasureDataMessageValidator.ValidateAsync((RequestAggregatedMeasureDataMessage)messageParserResult01.IncomingMessage!, CancellationToken.None);

        var messageParserResult02 = await ParseMessageAsync(message02);
        var result02 = await _requestAggregatedMeasureDataMessageValidator.ValidateAsync((RequestAggregatedMeasureDataMessage)messageParserResult02.IncomingMessage!, CancellationToken.None);

        Assert.DoesNotContain(result01.Errors, error => error is DuplicateMessageIdDetected);
        Assert.DoesNotContain(result02.Errors, error => error is DuplicateMessageIdDetected);
    }

    [Fact]
    public async Task Message_ids_must_not_exists_for_sender()
    {
        var senderActorNumber = "1234567890123";
        var existingMessageId = "123564789123564789123564789123564789";
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithMessageId(existingMessageId)
            .WithSenderId(senderActorNumber)
            .WithReceiverId("5790001330552")
            .Message();
        await StoreMessageIdForActorAsync(existingMessageId, senderActorNumber);

        var messageParserResult = await ParseMessageAsync(message);
        var result = await _requestAggregatedMeasureDataMessageValidator.ValidateAsync((RequestAggregatedMeasureDataMessage)messageParserResult.IncomingMessage!, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, error => error is DuplicateMessageIdDetected);
    }

    [Theory]
    [MemberData(nameof(AllowedActorRoles))]
    public async Task Sender_role_type_must_be_the_role_of(string role)
    {
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithSenderRole(role)
            .Message();

        var messageParserResult = await ParseMessageAsync(message);
        var result = await _requestAggregatedMeasureDataMessageValidator.ValidateAsync((RequestAggregatedMeasureDataMessage)messageParserResult.IncomingMessage!, CancellationToken.None);

        Assert.DoesNotContain(result.Errors, error => error is SenderRoleTypeIsNotAuthorized);
    }

    [Fact]
    public async Task Return_failure_if_xml_schema_for_business_reason_does_not_exist()
    {
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData("Infrastructure.CimMessageAdapter//Messages//Xml//BadRequestAggregatedMeasureData.xml")
            .Message();

        var messageParserResult = await ParseMessageAsync(message);

        Assert.False(messageParserResult.Success);
        Assert.Contains(messageParserResult.Errors, error => error is InvalidBusinessReasonOrVersion);
    }

    [Fact]
    public async Task Message_does_match_the_expected_schema()
    {
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData("Infrastructure.CimMessageAdapter//Messages//Xml//RequestChangeCustomerCharacteristics.xml")
            .Message();

        var messageParserResult = await ParseMessageAsync(message);

        Assert.False(messageParserResult.Success);
        Assert.Contains(messageParserResult.Errors, error => error is InvalidMessageStructure);
    }

    [Fact]
    public async Task Process_type_is_not_allowed()
    {
        var knownReceiverId = "5790001330552";
        var knownReceiverRole = "DDZ";
        var notAllowedProcessType = "1880";
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithProcessType(notAllowedProcessType)
            .WithReceiverRole(knownReceiverRole)
            .WithReceiverId(knownReceiverId)
            .Message();

        var messageParserResult = await ParseMessageAsync(message);

        Assert.False(messageParserResult.Success);
        Assert.Contains(messageParserResult.Errors, error => error is InvalidMessageStructure);
        Assert.Contains(messageParserResult.Errors, error => error.Message.Contains(new NotSupportedProcessType(string.Empty).Target!, StringComparison.InvariantCultureIgnoreCase));
    }

    [Fact]
    public async Task Message_type_is_not_allowed()
    {
        var knownReceiverId = "5790001330552";
        var knownReceiverRole = "DDZ";
        var notAllowedMessageType = "1880";
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithMessageType(notAllowedMessageType)
            .WithReceiverRole(knownReceiverRole)
            .WithReceiverId(knownReceiverId)
            .Message();

        var messageParserResult = await ParseMessageAsync(message);

        Assert.False(messageParserResult.Success);
        Assert.Contains(messageParserResult.Errors, error => error is InvalidMessageStructure);
        Assert.Contains(messageParserResult.Errors, error => error.Message.Contains(new NotSupportedMessageType(string.Empty).Target!, StringComparison.InvariantCultureIgnoreCase));
    }

    [Fact]
    public async Task Message_id_must_be_in_correct_length()
    {
        var knownReceiverId = "5790001330552";
        var knownReceiverRole = "DDZ";
        var toShortMessageId = "36";
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithMessageId(toShortMessageId)
            .WithReceiverRole(knownReceiverRole)
            .WithReceiverId(knownReceiverId)
            .Message();

        var messageParserResult = await ParseMessageAsync(message);
        var result = await _requestAggregatedMeasureDataMessageValidator.ValidateAsync((RequestAggregatedMeasureDataMessage)messageParserResult.IncomingMessage!, CancellationToken.None);

        Assert.Contains(result.Errors, error => error is InvalidMessageIdSize);
    }

    [Fact]
    public async Task Multiple_activity_records_are_committed_as_processes()
    {
        var knownReceiverId = "5790001330552";
        var knownReceiverRole = ActorRole.MeteredDataAdministrator.Code;
        var knownSenderId = "5790001330554";
        var knownSenderRole = ActorRole.EnergySupplier.Code;
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .DuplicateSeriesRecords()
            .WithSeriesTransactionId(Guid.NewGuid().ToString())
            .WithReceiverRole(knownReceiverRole)
            .WithReceiverId(knownReceiverId)
            .WithSenderId(knownSenderId)
            .WithSenderRole(knownSenderRole)
            .Message();

        var messageParserResult = await ParseMessageAsync(message);
        var marketMessage = CreateMarketMessageWithAuthentication((RequestAggregatedMeasureDataMessage)messageParserResult.IncomingMessage!, knownSenderId, knownSenderRole);
        await InvokeCommandAsync(new InitializeAggregatedMeasureDataProcessesCommand(marketMessage));

        var processes = _processContext.AggregatedMeasureDataProcesses.ToList();
        Assert.NotNull(processes);
        Assert.Equal(2, processes.Count);
    }

    [Fact]
    public async Task Transaction_id_must_not_be_less_than_36_characters()
    {
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithSeriesTransactionId("12356478912356478912356478912356478")
            .Message();

        var messageParserResult = await ParseMessageAsync(message);
        var result = await _requestAggregatedMeasureDataMessageValidator.ValidateAsync((RequestAggregatedMeasureDataMessage)messageParserResult.IncomingMessage!, CancellationToken.None);

        Assert.Contains(result.Errors, error => error is InvalidTransactionIdSize);
    }

    [Fact]
    public async Task Transaction_id_must_be_36_characters()
    {
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithSeriesTransactionId("123564789123564789123564789123564789")
            .Message();

        var messageParserResult = await ParseMessageAsync(message);
        var result = await _requestAggregatedMeasureDataMessageValidator.ValidateAsync((RequestAggregatedMeasureDataMessage)messageParserResult.IncomingMessage!, CancellationToken.None);

        Assert.DoesNotContain(result.Errors, error => error is InvalidTransactionIdSize);
    }

    [Fact]
    public async Task Transaction_id_must_not_be_more_than_36_characters()
    {
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithSeriesTransactionId("123564789123564789123564789123564789_123564789123564789123564789123564789")
            .Message();

        var messageParserResult = await ParseMessageAsync(message);
        var result = await _requestAggregatedMeasureDataMessageValidator.ValidateAsync((RequestAggregatedMeasureDataMessage)messageParserResult.IncomingMessage!, CancellationToken.None);

        Assert.Contains(result.Errors, error => error is InvalidTransactionIdSize);
    }

    [Fact]
    public async Task Business_type_is_allowed()
    {
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithBusinessType("23")
            .Message();

        var messageParserResult = await ParseMessageAsync(message);
        var result = await _requestAggregatedMeasureDataMessageValidator.ValidateAsync((RequestAggregatedMeasureDataMessage)messageParserResult.IncomingMessage!, CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task Business_type_is_not_allowed()
    {
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithBusinessType("27")
            .Message();

        var messageParserResult = await ParseMessageAsync(message);
        var result = await _requestAggregatedMeasureDataMessageValidator.ValidateAsync((RequestAggregatedMeasureDataMessage)messageParserResult.IncomingMessage!, CancellationToken.None);

        Assert.Contains(result.Errors, error => error is NotSupportedBusinessType);
    }

    private RequestAggregatedMeasureDataDto CreateMarketMessageWithAuthentication(RequestAggregatedMeasureDataMessage message, string knownSenderId, string knownSenderRole)
    {
        var authenticatedActor = GetService<AuthenticatedActor>();
        authenticatedActor.SetAuthenticatedActor(new ActorIdentity(ActorNumber.Create(knownSenderId), restriction: Restriction.None,  ActorRole.FromCode(knownSenderRole)));

        var series = message.Series
            .Select(
                serie => new Serie(
                    serie.Id,
                    serie.MarketEvaluationPointType,
                    serie.MarketEvaluationSettlementMethod,
                    serie.StartDateAndOrTimeDateTime,
                    serie.EndDateAndOrTimeDateTime,
                    serie.MeteringGridAreaDomainId,
                    serie.EnergySupplierMarketParticipantId,
                    serie.BalanceResponsiblePartyMarketParticipantId,
                    serie.SettlementSeriesVersion)).ToList().AsReadOnly();
        return new RequestAggregatedMeasureDataDto(
            message.SenderNumber,
            message.SenderRoleCode,
            message.ReceiverNumber,
            message.ReceiverRoleCode,
            message.BusinessReason,
            message.MessageType,
            message.MessageId,
            message.CreatedAt,
            message.BusinessType,
            series);
    }

    private Task<IncomingMarketMessageParserResult> ParseMessageAsync(Stream message)
    {
        return _marketMessageParser.ParseAsync(new IncomingMessageStream(message), DocumentFormat.Xml, IncomingDocumentType.RequestAggregatedMeasureData, CancellationToken.None);
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
