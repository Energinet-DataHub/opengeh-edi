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
using CimMessageAdapter.Errors;
using CimMessageAdapter.Messages;
using CimMessageAdapter.Messages.RequestAggregatedMeasureData;
using Domain.Actors;
using Domain.Documents;
using Infrastructure.Configuration.Authentication;
using IntegrationTests.CimMessageAdapter.Stubs;
using IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;

namespace IntegrationTests.CimMessageAdapter.Messages.RequestAggregatedMeasureData;

[IntegrationTest]
public class RequestAggregatedMeasureDataReceiverTests : TestBase, IAsyncLifetime
{
    private readonly MessageParser _messageParser;
    private readonly IMarketActorAuthenticator _marketActorAuthenticator;
    private readonly ITransactionIds _transactionIds;
    private readonly IMessageIds _messageIds;
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
    }

    public async Task InitializeAsync()
    {
        await InvokeCommandAsync(new CreateActor(Guid.NewGuid().ToString(), SampleData.StsAssignedUserId, SampleData.SenderId)).ConfigureAwait(false);
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
        var knownReceiverRole = "DDZ";
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithReceiverRole(knownReceiverRole)
            .WithReceiverId(unknownReceiverId)
            .Message();

        //TODO: MessageParser should return an object representing the request document. consider RequestAggregatedMeasureDataIncomingMarketDocument
        var messageParserResult = await ParseMessageAsync(message).ConfigureAwait(false);

        //TODO: triggers MessageReceiver for this request. Which is responsible for validating and forwarding the request to 3th part systems. Consider splitting the responsibilities into 2 (MessageValidator and "MessageDispatcher").
        //TODO: RequestAggregatedMeasureDataReceiver should come from the IOC
        var result = await CreateMessageReceiver().ReceiveAsync(messageParserResult, CancellationToken.None).ConfigureAwait(false);

        Assert.Contains(result.Errors, error => error is UnknownReceiver);
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

        Assert.DoesNotContain(result.Errors, error => error is UnknownReceiver);
    }

    [Fact]
    public async Task Receiver_role_must_be_metering_point_administrator()
    {
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithReceiverRole("DDZ")
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
        var result = await CreateMessageReceiver().ReceiveAsync(messageParserResult, CancellationToken.None).ConfigureAwait(false);

        Assert.Contains(result.Errors, error => error is InvalidReceiverRole);
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

        Assert.DoesNotContain(result.Errors, error => error is SenderIdDoesNotMatchAuthenticatedUser);
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

        Assert.Contains(result.Errors, error => error is SenderIdDoesNotMatchAuthenticatedUser);
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

    private async Task CreateIdentityWithRoles(IEnumerable<MarketRole> roles)
    {
        var claims = new List<Claim>(_claims);
        claims.AddRange(roles.Select(ClaimsMap.RoleFrom));
        await _marketActorAuthenticator
            .AuthenticateAsync(new ClaimsPrincipal(new ClaimsIdentity(claims)), CancellationToken.None)
            .ConfigureAwait(false);
    }

    private MessageReceiver<global::CimMessageAdapter.Messages.Queues.RequestAggregatedMeasureDataTransaction> CreateMessageReceiver()
    {
        var messageQueueDispatcherSpy = new MessageQueueDispatcherStub<global::CimMessageAdapter.Messages.Queues.RequestAggregatedMeasureDataTransaction>();
        var messageReceiver = new RequestAggregatedMeasureDataReceiver(
            _messageIds,
            messageQueueDispatcherSpy,
            _transactionIds,
            new SenderAuthorizer(_marketActorAuthenticator));
        return messageReceiver;
    }

    private Task<MessageParserResult<Serie, RequestAggregatedMeasureDataTransaction>> ParseMessageAsync(Stream message)
    {
        return _messageParser.ParseAsync(message, DocumentFormat.Xml, CancellationToken.None);
    }
}
