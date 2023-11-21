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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Energinet.DataHub.EDI.Application.Actors;
using Energinet.DataHub.EDI.Application.Configuration.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.Common;
using Energinet.DataHub.EDI.Common.Actors;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.IntegrationTests.TestDoubles;
using Energinet.DataHub.EDI.Process.Application.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.Process.Interfaces;
using IncomingMessages.Infrastructure;
using IncomingMessages.Infrastructure.Messages;
using IncomingMessages.Infrastructure.Messages.RequestAggregatedMeasureData;
using IncomingMessages.Infrastructure.ValidationErrors;
using Xunit;

namespace Energinet.DataHub.EDI.IntegrationTests.Infrastructure.CimMessageAdapter.Messages.RequestAggregatedMeasureData;

public class RequestAggregatedMeasureDataReceiverTests : TestBase, IAsyncLifetime
{
    private readonly RequestAggregatedMeasureDataMarketMessageParser _requestAggregatedMeasureDataMarketMessageParser;
    private readonly ITransactionIdRepository _transactionIdRepository;
    private readonly IMessageIdRepository _messageIdRepository;
    private readonly ProcessTypeValidator _processTypeValidator;
    private readonly MessageTypeValidator _messageTypeValidator;
    private readonly CalculationResponsibleReceiverVerification _calculationResponsibleReceiverVerification;
    private readonly ProcessContext _processContext;
    private readonly IMarketActorAuthenticator _marketActorAuthenticator;
    private readonly BusinessTypeValidator _businessTypeValidator;

    public RequestAggregatedMeasureDataReceiverTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _requestAggregatedMeasureDataMarketMessageParser = GetService<RequestAggregatedMeasureDataMarketMessageParser>();
        _transactionIdRepository = GetService<ITransactionIdRepository>();
        _messageIdRepository = GetService<IMessageIdRepository>();
        _processTypeValidator = GetService<ProcessTypeValidator>();
        _messageTypeValidator = GetService<MessageTypeValidator>();
        _calculationResponsibleReceiverVerification = GetService<CalculationResponsibleReceiverVerification>();
        _processContext = GetService<ProcessContext>();
        _marketActorAuthenticator = new MarketActorAuthenticatorSpy(ActorNumber.Create("1234567890123"), "DDQ");
        _businessTypeValidator = GetService<BusinessTypeValidator>();
    }

    public static IEnumerable<object[]> AllowedActorRoles =>
        new List<object[]>
        {
            new object[] { MarketRole.EnergySupplier.Code },
            new object[] { MarketRole.MeteredDataResponsible.Code },
            new object[] { MarketRole.BalanceResponsibleParty.Code },
        };

    public async Task InitializeAsync()
    {
#pragma warning disable CA2007

        await InvokeCommandAsync(new CreateActorCommand(SampleData.StsAssignedUserId, ActorNumber.Create(SampleData.SenderId)));
        await InvokeCommandAsync(new CreateActorCommand(SampleData.SecondStsAssignedUserId, ActorNumber.Create(SampleData.SecondSenderId)));
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
        var result = await CreateMessageReceiver().ValidateAsync(messageParserResult.MarketMessage!, CancellationToken.None);

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
        var result = await CreateMessageReceiver().ValidateAsync(messageParserResult.MarketMessage!, CancellationToken.None);

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
        var result = await CreateMessageReceiver().ValidateAsync(messageParserResult.MarketMessage!, CancellationToken.None);

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
        var result = await CreateMessageReceiver().ValidateAsync(messageParserResult.MarketMessage!, CancellationToken.None);

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
        var result = await CreateMessageReceiver().ValidateAsync(messageParserResult.MarketMessage!, CancellationToken.None);

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
        var result = await CreateMessageReceiver().ValidateAsync(messageParserResult.MarketMessage!, CancellationToken.None);

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
        var result = await CreateMessageReceiver().ValidateAsync(messageParserResult.MarketMessage!, CancellationToken.None);

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
        var resultFromFirstMessage = await CreateMessageReceiver().ValidateAsync(messageParserResult.MarketMessage!, CancellationToken.None);

        // Request from a second sender.
        await using var message02 = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithReceiverRole(knownReceiverRole)
            .WithReceiverId(knownReceiverId)
            .WithMessageId("123564789123564789123564789123564789")
            .Message();

        var messageParserResult2 = await ParseMessageAsync(message02);
        var resultFromSecondMessage = await CreateMessageReceiver().ValidateAsync(messageParserResult2.MarketMessage!, CancellationToken.None);

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
        var result = await CreateMessageReceiver().ValidateAsync(messageParserResult.MarketMessage!, CancellationToken.None);

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
        var result = await CreateMessageReceiver().ValidateAsync(messageParserResult.MarketMessage!, CancellationToken.None);

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
        var result01 = await CreateMessageReceiver().ValidateAsync(messageParserResult01.MarketMessage!, CancellationToken.None);

        var messageParserResult02 = await ParseMessageAsync(message02);
        var result02 = await CreateMessageReceiver().ValidateAsync(messageParserResult02.MarketMessage!, CancellationToken.None);

        Assert.DoesNotContain(result01.Errors, error => error is DuplicateMessageIdDetected);
        Assert.DoesNotContain(result02.Errors, error => error is DuplicateMessageIdDetected);
    }

    [Fact]
    public async Task Message_ids_must_be_unique_for_sender()
    {
        await using var message01 = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithReceiverId("5790001330552")
            .Message();

        var messageParserResult = await ParseMessageAsync(message01);
        var result01 = await CreateMessageReceiver().ValidateAsync(messageParserResult.MarketMessage!, CancellationToken.None);
        var result02 = await CreateMessageReceiver().ValidateAsync(messageParserResult.MarketMessage!, CancellationToken.None);

        Assert.True(result01.Success);
        Assert.DoesNotContain(result01.Errors, error => error is DuplicateMessageIdDetected);
        Assert.Contains(result02.Errors, error => error is DuplicateMessageIdDetected);
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
        var result = await CreateMessageReceiver().ValidateAsync(messageParserResult.MarketMessage!, CancellationToken.None);

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
        var result = await CreateMessageReceiver().ValidateAsync(messageParserResult.MarketMessage!, CancellationToken.None);

        Assert.Contains(result.Errors, error => error is InvalidMessageIdSize);
    }

    [Fact]
    public async Task Valid_activity_records_are_extracted_and_committed_as_a_process()
    {
        var knownReceiverId = "5790001330552";
        var knownReceiverRole = "DGL";
        var knownSenderId = "5790001330551";
        var knownSenderRole = MarketRole.EnergySupplier.Code;
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithReceiverRole(knownReceiverRole)
            .WithReceiverId(knownReceiverId)
            .WithSenderId(knownSenderId)
            .WithSenderRole(knownSenderRole)
            .Message();

        var messageParserResult = await ParseMessageAsync(message);
        var marketMessage = CreateMarketMessageWithAuthentication(messageParserResult.MarketMessage!, knownSenderId, knownSenderRole);
        await InvokeCommandAsync(new InitializeAggregatedMeasureDataProcessesCommand(marketMessage));

        var process = _processContext.AggregatedMeasureDataProcesses.FirstOrDefault();
        Assert.NotNull(process);
    }

    [Fact]
    public async Task Multiple_activity_records_are_committed_as_processes()
    {
        var knownReceiverId = "5790001330552";
        var knownReceiverRole = MarketRole.CalculationResponsibleRole.Code;
        var knownSenderId = "5790001330554";
        var knownSenderRole = MarketRole.EnergySupplier.Code;
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
        var marketMessage = CreateMarketMessageWithAuthentication(messageParserResult.MarketMessage!, knownSenderId, knownSenderRole);
        await InvokeCommandAsync(new InitializeAggregatedMeasureDataProcessesCommand(marketMessage));

        var processes = _processContext.AggregatedMeasureDataProcesses.ToList();
        Assert.NotNull(processes);
        Assert.Equal(2, messageParserResult.MarketMessage!.Series.Count);
    }

    [Fact]
    public async Task Transaction_and_message_ids_are_not_saved_when_receiving_a_faulted_request()
    {
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithReceiverId("5790001330552") // This is not MDR
            .WithReceiverRole("MDR")
            .Message();

        var messageParserResult = await ParseMessageAsync(message);

        // Act
        var result = await CreateMessageReceiver().ValidateAsync(messageParserResult.MarketMessage!, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);

        await AssertTransactionIdIsNotStoredAsync(messageParserResult.MarketMessage!.SenderNumber, messageParserResult.MarketMessage!.Series.First().Id);
        await AssertMessageIdIsNotStoredAsync(messageParserResult.MarketMessage!.SenderNumber, messageParserResult.MarketMessage!.MessageId);
    }

    [Fact]
    public async Task Transaction_id_must_not_be_less_than_36_characters()
    {
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithSeriesTransactionId("12356478912356478912356478912356478")
            .Message();

        var messageParserResult = await ParseMessageAsync(message);
        var result = await CreateMessageReceiver().ValidateAsync(messageParserResult.MarketMessage!, CancellationToken.None);

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
        var result = await CreateMessageReceiver().ValidateAsync(messageParserResult.MarketMessage!, CancellationToken.None);

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
        var result = await CreateMessageReceiver().ValidateAsync(messageParserResult.MarketMessage!, CancellationToken.None);

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
        var result = await CreateMessageReceiver().ValidateAsync(messageParserResult.MarketMessage!, CancellationToken.None);

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
        var result = await CreateMessageReceiver().ValidateAsync(messageParserResult.MarketMessage!, CancellationToken.None);

        Assert.Contains(result.Errors, error => error is NotSupportedBusinessType);
    }

    private static RequestAggregatedMeasureDataMarketMessage CreateMarketMessageWithAuthentication(RequestAggregatedMeasureDataMarketMessage marketMessage, string knownSenderId, string knownSenderRole)
    {
        return new RequestAggregatedMeasureDataMarketMessage(
            marketMessage.SenderNumber,
            marketMessage.SenderRoleCode,
            marketMessage.ReceiverNumber,
            marketMessage.ReceiverRoleCode,
            marketMessage.BusinessReason,
            knownSenderId,
            knownSenderRole,
            marketMessage.MessageType,
            marketMessage.MessageId,
            marketMessage.CreatedAt,
            marketMessage.BusinessType,
            marketMessage.Series);
    }

    private RequestAggregatedMeasureDataValidator CreateMessageReceiver()
    {
        var messageReceiver = new RequestAggregatedMeasureDataValidator(
            _messageIdRepository,
            _transactionIdRepository,
            new SenderAuthorizer(_marketActorAuthenticator),
            _processTypeValidator,
            _messageTypeValidator,
            _calculationResponsibleReceiverVerification,
            _businessTypeValidator);
        return messageReceiver;
    }

    private Task<RequestAggregatedMeasureDataMarketMessageParserResult> ParseMessageAsync(Stream message)
    {
        return _requestAggregatedMeasureDataMarketMessageParser.ParseAsync(message, DocumentFormat.Xml, CancellationToken.None);
    }

    private async Task AssertTransactionIdIsStored(string senderId, string transactionId)
    {
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var sql =
            "SELECT * FROM dbo.TransactionRegistry WHERE TransactionId = @TransactionId AND SenderId = @SenderId";
        var transaction = await connection.QueryFirstOrDefaultAsync(sql, new { TransactionId = transactionId, SenderId = senderId });
        Assert.NotNull(transaction);
    }

    private async Task AssertTransactionIdIsNotStoredAsync(string senderId, string transactionId)
    {
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var sql =
            "SELECT * FROM dbo.TransactionRegistry WHERE TransactionId = @TransactionId AND SenderId = @SenderId";
        var transaction = await connection.QueryFirstOrDefaultAsync(sql, new { TransactionId = transactionId, SenderId = senderId });
        Assert.Null(transaction);
    }

    private async Task AssertMessageIdIsStored(string senderId, string messageId)
    {
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var sql =
            "SELECT [MessageId], [SenderId] FROM dbo.MessageRegistry WHERE MessageId = @MessageId AND SenderId = @SenderId";
        var transaction = await connection.QueryFirstOrDefaultAsync<MessageIdForSender>(sql, new { MessageId = messageId, SenderId = senderId });
        Assert.NotNull(transaction);
    }

    private async Task AssertMessageIdIsNotStoredAsync(string senderId, string messageId)
    {
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var sql =
            "SELECT * FROM dbo.MessageRegistry WHERE MessageId = @MessageId AND SenderId = @SenderId";
        var message = await connection.QueryFirstOrDefaultAsync(sql, new { MessageId = messageId, SenderId = senderId });
        Assert.Null(message);
    }
}
