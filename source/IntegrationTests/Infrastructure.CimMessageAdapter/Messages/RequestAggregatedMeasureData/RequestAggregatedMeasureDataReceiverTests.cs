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
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Application.Actors;
using Application.Configuration.Authentication;
using Application.IncomingMessages.RequestAggregatedMeasureData;
using CimMessageAdapter.Messages;
using CimMessageAdapter.Messages.RequestAggregatedMeasureData;
using CimMessageAdapter.ValidationErrors;
using Domain.Actors;
using Domain.Documents;
using Infrastructure.Configuration.Authentication;
using IntegrationTests.Fixtures;
using IntegrationTests.Infrastructure.CimMessageAdapter.Stubs;
using Xunit;
using Xunit.Categories;

namespace IntegrationTests.Infrastructure.CimMessageAdapter.Messages.RequestAggregatedMeasureData;

[IntegrationTest]
public class RequestAggregatedMeasureDataReceiverTests : TestBase, IAsyncLifetime
{
    private readonly MessageParser _messageParser;
    private readonly IMarketActorAuthenticator _marketActorAuthenticator;
    private readonly ITransactionIds _transactionIds;
    private readonly IMessageIds _messageIds;
    private readonly ProcessTypeValidator _processTypeValidator;
    private readonly MessageTypeValidator _messageTypeValidator;
    private readonly CalculationResponsibleReceiverVerification _calculationResponsibleReceiverVerification;
    private readonly MessageQueueDispatcherStub<global::CimMessageAdapter.Messages.Queues.RequestAggregatedMeasureDataTransactionQueues> _messageQueueDispatcherSpy = new();
    private readonly List<Claim> _claims = new()
    {
        new(ClaimsMap.UserId, new CreateActor(Guid.NewGuid().ToString(), SampleData.StsAssignedUserId, SampleData.SenderId).B2CId),
    };

    public RequestAggregatedMeasureDataReceiverTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _messageParser = GetService<MessageParser>();
        _transactionIds = GetService<ITransactionIds>();
        _messageIds = GetService<IMessageIds>();
        _marketActorAuthenticator = GetService<IMarketActorAuthenticator>();
        _processTypeValidator = GetService<ProcessTypeValidator>();
        _messageTypeValidator = GetService<MessageTypeValidator>();
        _calculationResponsibleReceiverVerification = GetService<CalculationResponsibleReceiverVerification>();
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

        await InvokeCommandAsync(new CreateActor(Guid.NewGuid().ToString(), SampleData.StsAssignedUserId, SampleData.SenderId)).ConfigureAwait(false);
        await InvokeCommandAsync(new CreateActor(Guid.NewGuid().ToString(), SampleData.SecondStsAssignedUserId, SampleData.SecondSenderId)).ConfigureAwait(false);
        //TODO: Consider removing authentication from validation (message receiver).
        await _marketActorAuthenticator.AuthenticateAsync(new ClaimsPrincipal(new ClaimsIdentity(_claims)), CancellationToken.None).ConfigureAwait(false);
    }

    public Task DisposeAsync()
    {
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

        var messageParserResult = await ParseMessageAsync(message).ConfigureAwait(false);

        var result = await CreateMessageReceiver().ReceiveAsync(messageParserResult, CancellationToken.None).ConfigureAwait(false);

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

        var messageParserResult = await ParseMessageAsync(message).ConfigureAwait(false);
        var result = await CreateMessageReceiver().ReceiveAsync(messageParserResult, CancellationToken.None).ConfigureAwait(false);

        Assert.DoesNotContain(result.Errors, error => error is InvalidReceiverId);
    }

    [Fact]
    public async Task Receiver_role_must_be_calculation_responsible()
    {
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithReceiverRole("DGL")
            .Message();

        var messageParserResult = await ParseMessageAsync(message).ConfigureAwait(false);
        var result = await CreateMessageReceiver().ReceiveAsync(messageParserResult, CancellationToken.None).ConfigureAwait(false);

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

        var messageParserResult = await ParseMessageAsync(message).ConfigureAwait(false);

        Assert.False(messageParserResult.Success);
        Assert.Contains(messageParserResult.Errors, error => error is InvalidMessageStructure);
        Assert.Contains(messageParserResult.Errors, error => error.Message.Contains(new InvalidReceiverRole().Target!, StringComparison.InvariantCultureIgnoreCase));
    }

    [Fact]
    public async Task Sender_id_must_match_the_organization_of_the_current_authenticated_user()
    {
        await CreateIdentityWithRoles(new List<MarketRole> { MarketRole.EnergySupplier })
            .ConfigureAwait(false);
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithSenderId(SampleData.SenderId)
            .Message();

        var messageParserResult = await ParseMessageAsync(message).ConfigureAwait(false);
        var result = await CreateMessageReceiver().ReceiveAsync(messageParserResult, CancellationToken.None).ConfigureAwait(false);

        Assert.DoesNotContain(result.Errors, error => error is AuthenticatedUserDoesNotMatchSenderId);
    }

    [Fact]
    public async Task Sender_id_does_not_match_the_current_authenticated_user()
    {
        await CreateIdentityWithRoles(new List<MarketRole> { MarketRole.EnergySupplier })
            .ConfigureAwait(false);
        var invalidSenderId = "5790001330550";
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithSenderId(invalidSenderId)
            .Message();

        var messageParserResult = await ParseMessageAsync(message).ConfigureAwait(false);
        var result = await CreateMessageReceiver().ReceiveAsync(messageParserResult, CancellationToken.None).ConfigureAwait(false);

        Assert.Contains(result.Errors, error => error is AuthenticatedUserDoesNotMatchSenderId);
    }

    [Fact]
    public async Task Authenticated_user_must_hold_the_role_type_as_specified_in_message()
    {
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .Message();

        var messageParserResult = await ParseMessageAsync(message).ConfigureAwait(false);
        var result = await CreateMessageReceiver().ReceiveAsync(messageParserResult, CancellationToken.None).ConfigureAwait(false);

        Assert.Contains(result.Errors, error => error is AuthenticatedUserDoesNotHoldRequiredRoleType);
    }

    [Fact]
    public async Task Series_must_have_unique_transaction_ids()
    {
        var knownReceiverId = "5790001330552";
        var knownReceiverRole = "DDZ";
        await CreateIdentityWithRoles(new List<MarketRole> { MarketRole.EnergySupplier })
            .ConfigureAwait(false);
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithReceiverRole(knownReceiverRole)
            .WithReceiverId(knownReceiverId)
            .WithSenderRole(MarketRole.EnergySupplier.Code)
            .WithSenderId(SampleData.SenderId)
            .DuplicateSeriesRecords()
            .Message();

        var messageParserResult = await ParseMessageAsync(message).ConfigureAwait(false);
        var result = await CreateMessageReceiver().ReceiveAsync(messageParserResult, CancellationToken.None).ConfigureAwait(false);

        Assert.Contains(result.Errors, error => error is DuplicateTransactionIdDetected);
    }

    [Fact]
    public async Task Series_must_have_unique_transaction_ids_across_senders()
    {
        var knownReceiverId = "5790001330552";
        var knownReceiverRole = "DDZ";
        await CreateIdentityWithRoles(new List<MarketRole> { MarketRole.EnergySupplier })
            .ConfigureAwait(false);
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithReceiverRole(knownReceiverRole)
            .WithReceiverId(knownReceiverId)
            .WithSenderRole(MarketRole.EnergySupplier.Code)
            .WithSenderId(SampleData.SenderId)
            .Message();

        var messageParserResult = await ParseMessageAsync(message).ConfigureAwait(false);
        var resultFromFirstMessage = await CreateMessageReceiver().ReceiveAsync(messageParserResult, CancellationToken.None).ConfigureAwait(false);

        // Request from a second sender.
        await using var message02 = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithReceiverRole(knownReceiverRole)
            .WithReceiverId(knownReceiverId)
            .WithMessageId("123564789123564789123564789123564789")
            .WithSenderRole(MarketRole.EnergySupplier.Code)
            .WithSenderId(SampleData.SecondSenderId)
            .Message();

        await CreateSecondIdentityWithRoles(new List<MarketRole> { MarketRole.EnergySupplier }, SampleData.SecondSenderId, SampleData.SecondStsAssignedUserId)
            .ConfigureAwait(false);
        var messageParserResult2 = await ParseMessageAsync(message02).ConfigureAwait(false);
        var resultFromSecondMessage = await CreateMessageReceiver().ReceiveAsync(messageParserResult2, CancellationToken.None).ConfigureAwait(false);

        Assert.DoesNotContain(resultFromFirstMessage.Errors, error => error is DuplicateTransactionIdDetected);
        Assert.DoesNotContain(resultFromSecondMessage.Errors, error => error is DuplicateTransactionIdDetected);
    }

    [Fact]
    public async Task Series_must_have_none_empty_transaction_ids()
    {
        var knownReceiverId = "5790001330552";
        var knownReceiverRole = "DDZ";
        await CreateIdentityWithRoles(new List<MarketRole> { MarketRole.EnergySupplier })
            .ConfigureAwait(false);
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithReceiverRole(knownReceiverRole)
            .WithReceiverId(knownReceiverId)
            .WithSenderRole(MarketRole.EnergySupplier.Code)
            .WithSenderId(SampleData.SenderId)
            .WithSeriesTransactionId(string.Empty)
            .Message();

        var messageParserResult = await ParseMessageAsync(message).ConfigureAwait(false);
        var result = await CreateMessageReceiver().ReceiveAsync(messageParserResult, CancellationToken.None).ConfigureAwait(false);

        Assert.Contains(result.Errors, error => error is EmptyTransactionId);
    }

    [Fact]
    public async Task Message_id_must_not_be_empty()
    {
        await CreateIdentityWithRoles(new List<MarketRole> { MarketRole.EnergySupplier })
            .ConfigureAwait(false);
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithMessageId(string.Empty)
            .Message();

        var messageParserResult = await ParseMessageAsync(message).ConfigureAwait(false);
        var result = await CreateMessageReceiver().ReceiveAsync(messageParserResult, CancellationToken.None).ConfigureAwait(false);

        Assert.Contains(result.Errors, error => error is EmptyMessageId);
    }

    [Fact]
    public async Task Message_ids_must_be_unique()
    {
        await CreateIdentityWithRoles(new List<MarketRole> { MarketRole.EnergySupplier })
            .ConfigureAwait(false);
        await using var message01 = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .Message();
        await using var message02 = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithSenderId(SampleData.SecondSenderId)
            .Message();

        var messageParserResult01 = await ParseMessageAsync(message01).ConfigureAwait(false);
        var result01 = await CreateMessageReceiver().ReceiveAsync(messageParserResult01, CancellationToken.None).ConfigureAwait(false);

        await CreateSecondIdentityWithRoles(new List<MarketRole> { MarketRole.EnergySupplier }, SampleData.SecondSenderId, SampleData.SecondStsAssignedUserId)
            .ConfigureAwait(false);
        var messageParserResult02 = await ParseMessageAsync(message02).ConfigureAwait(false);
        var result02 = await CreateMessageReceiver().ReceiveAsync(messageParserResult02, CancellationToken.None).ConfigureAwait(false);

        Assert.DoesNotContain(result01.Errors, error => error is DuplicateMessageIdDetected);
        Assert.DoesNotContain(result02.Errors, error => error is DuplicateMessageIdDetected);
    }

    [Fact]
    public async Task Message_ids_must_not_be_unique_across_senders()
    {
        await CreateIdentityWithRoles(new List<MarketRole> { MarketRole.EnergySupplier })
            .ConfigureAwait(false);
        await using var message01 = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .Message();
        await using var message02 = BusinessMessageBuilder
            .RequestAggregatedMeasureData()

            .Message();

        var messageParserResult01 = await ParseMessageAsync(message01).ConfigureAwait(false);
        var result01 = await CreateMessageReceiver().ReceiveAsync(messageParserResult01, CancellationToken.None).ConfigureAwait(false);

        var messageParserResult02 = await ParseMessageAsync(message01).ConfigureAwait(false);
        var result02 = await CreateMessageReceiver().ReceiveAsync(messageParserResult02, CancellationToken.None).ConfigureAwait(false);

        Assert.DoesNotContain(result01.Errors, error => error is DuplicateMessageIdDetected);
        Assert.Contains(result02.Errors, error => error is DuplicateMessageIdDetected);
    }

    [Fact]
    public async Task Series_must_have_unique_message_ids()
    {
        await CreateIdentityWithRoles(new List<MarketRole> { MarketRole.EnergySupplier })
            .ConfigureAwait(false);
        await using var message01 = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .Message();
        await using var message02 = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .Message();

        var messageParserResult01 = await ParseMessageAsync(message01).ConfigureAwait(false);
        var result01 = await CreateMessageReceiver().ReceiveAsync(messageParserResult01, CancellationToken.None).ConfigureAwait(false);

        var messageParserResult02 = await ParseMessageAsync(message01).ConfigureAwait(false);
        var result02 = await CreateMessageReceiver().ReceiveAsync(messageParserResult02, CancellationToken.None).ConfigureAwait(false);

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

        var messageParserResult = await ParseMessageAsync(message).ConfigureAwait(false);
        var result = await CreateMessageReceiver().ReceiveAsync(messageParserResult, CancellationToken.None).ConfigureAwait(false);

        Assert.DoesNotContain(result.Errors, error => error is SenderRoleTypeIsNotAuthorized);
    }

    [Fact]
    public async Task Return_failure_if_xml_schema_for_business_reason_does_not_exist()
    {
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData("Infrastructure.CimMessageAdapter//Messages//Xml//BadRequestAggregatedMeasureData.xml")
            .Message();

        var messageParserResult = await ParseMessageAsync(message).ConfigureAwait(false);
        var result = await CreateMessageReceiver().ReceiveAsync(messageParserResult, CancellationToken.None).ConfigureAwait(false);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, error => error is InvalidBusinessReasonOrVersion);
    }

    [Fact]
    public async Task Message_does_match_the_expected_schema()
    {
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData("Infrastructure.CimMessageAdapter//Messages//Xml//RequestChangeCustomerCharacteristics.xml")
            .Message();

        var messageParserResult = await ParseMessageAsync(message).ConfigureAwait(false);
        var result = await CreateMessageReceiver().ReceiveAsync(messageParserResult, CancellationToken.None).ConfigureAwait(false);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, error => error is InvalidMessageStructure);
    }

    [Fact]
    public async Task Process_type_is_not_allowed()
    {
        var knownReceiverId = "5790001330552";
        var knownReceiverRole = "DDZ";
        var notAllowedProcessType = "1880";
        await CreateIdentityWithRoles(new List<MarketRole> { MarketRole.EnergySupplier })
            .ConfigureAwait(false);
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithProcessType(notAllowedProcessType)
            .WithReceiverRole(knownReceiverRole)
            .WithReceiverId(knownReceiverId)
            .WithSenderRole(MarketRole.EnergySupplier.Code)
            .WithSenderId(SampleData.SenderId)
            .Message();

        var messageParserResult = await ParseMessageAsync(message).ConfigureAwait(false);

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
        await CreateIdentityWithRoles(new List<MarketRole> { MarketRole.EnergySupplier })
            .ConfigureAwait(false);
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithMessageType(notAllowedMessageType)
            .WithReceiverRole(knownReceiverRole)
            .WithReceiverId(knownReceiverId)
            .WithSenderRole(MarketRole.EnergySupplier.Code)
            .WithSenderId(SampleData.SenderId)
            .Message();

        var messageParserResult = await ParseMessageAsync(message).ConfigureAwait(false);

        Assert.False(messageParserResult.Success);
        Assert.Contains(messageParserResult.Errors, error => error is InvalidMessageStructure);
    }

    [Fact]
    public async Task Message_id_must_be_in_correct_length()
    {
        var knownReceiverId = "5790001330552";
        var knownReceiverRole = "DDZ";
        await CreateIdentityWithRoles(new List<MarketRole> { MarketRole.EnergySupplier })
            .ConfigureAwait(false);
        var toShortMessageId = "36";
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithMessageId(toShortMessageId)
            .WithSenderRole(MarketRole.EnergySupplier.Code)
            .WithSenderId(SampleData.SenderId)
            .WithReceiverRole(knownReceiverRole)
            .WithReceiverId(knownReceiverId)
            .Message();

        var messageParserResult = await ParseMessageAsync(message).ConfigureAwait(false);
        var result = await CreateMessageReceiver().ReceiveAsync(messageParserResult, CancellationToken.None).ConfigureAwait(false);

        Assert.Contains(result.Errors, error => error is InvalidMessageIdSize);
    }

    [Fact]
    public async Task Valid_activity_records_are_extracted_and_committed_to_queue()
    {
        await CreateIdentityWithRoles(new List<MarketRole> { MarketRole.EnergySupplier })
            .ConfigureAwait(false);
        var knownReceiverId = "5790001330552";
        var knownReceiverRole = "DGL";
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithSenderId(SampleData.SenderId)
            .WithSenderRole(MarketRole.EnergySupplier.Code)
            .WithSenderId(SampleData.SenderId)
            .WithReceiverRole(knownReceiverRole)
            .WithReceiverId(knownReceiverId)
            .Message();

        var messageParserResult = await ParseMessageAsync(message).ConfigureAwait(false);
        var result = await CreateMessageReceiver().ReceiveAsync(messageParserResult, CancellationToken.None).ConfigureAwait(false);

        var transaction = _messageQueueDispatcherSpy.CommittedItems.FirstOrDefault();
        Assert.True(result.Success);
        Assert.NotNull(transaction);
    }

    private async Task CreateIdentityWithRoles(IEnumerable<MarketRole> roles)
    {
        var claims = new List<Claim>(_claims);
        claims.AddRange(roles.Select(ClaimsMap.RoleFrom));
        await _marketActorAuthenticator
            .AuthenticateAsync(new ClaimsPrincipal(new ClaimsIdentity(claims)), CancellationToken.None)
            .ConfigureAwait(false);
    }

    private async Task CreateSecondIdentityWithRoles(IEnumerable<MarketRole> roles, string senderId, string b2cId)
    {
        List<Claim> claims = new()
        {
            new(
                ClaimsMap.UserId,
                new CreateActor(Guid.NewGuid().ToString(), b2cId, senderId).B2CId),
        };
        claims.AddRange(roles.Select(ClaimsMap.RoleFrom));
        await _marketActorAuthenticator
            .AuthenticateAsync(new ClaimsPrincipal(new ClaimsIdentity(claims)), CancellationToken.None)
            .ConfigureAwait(false);
    }

    private MessageReceiver<global::CimMessageAdapter.Messages.Queues.RequestAggregatedMeasureDataTransactionQueues> CreateMessageReceiver()
    {
        var messageReceiver = new RequestAggregatedMeasureDataReceiver(
            _messageIds,
            _messageQueueDispatcherSpy,
            _transactionIds,
            new SenderAuthorizer(_marketActorAuthenticator),
            _processTypeValidator,
            _messageTypeValidator,
            _calculationResponsibleReceiverVerification);
        return messageReceiver;
    }

    private Task<MessageParserResult<Serie, RequestAggregatedMeasureDataTransaction>> ParseMessageAsync(Stream message)
    {
        return _messageParser.ParseAsync(message, DocumentFormat.Xml, CancellationToken.None);
    }
}
