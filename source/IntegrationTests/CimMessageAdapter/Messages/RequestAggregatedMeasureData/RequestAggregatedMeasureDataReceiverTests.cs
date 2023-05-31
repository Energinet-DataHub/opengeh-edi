using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Application.Configuration.Authentication;
using Application.IncomingMessages.RequestAggregatedMeasureData;
using CimMessageAdapter.Errors;
using CimMessageAdapter.Messages;
using CimMessageAdapter.Messages.RequestAggregatedMeasureData;
using Domain.Documents;
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

    public RequestAggregatedMeasureDataReceiverTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _messageParser = GetService<MessageParser>();
        _transactionIds = GetService<ITransactionIds>();
        _messageIds = GetService<IMessageIds>();
        _marketActorAuthenticator = GetService<IMarketActorAuthenticator>();
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
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
        var result = await ReceiveRequestChangeOfSupplierMessage(messageParserResult).ConfigureAwait(false);

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
        var result = await ReceiveRequestChangeOfSupplierMessage(messageParserResult).ConfigureAwait(false);

        Assert.Contains(result.Errors, error => error is not UnknownReceiver);
    }

    private async Task<Result> ReceiveRequestChangeOfSupplierMessage(MessageParserResult<Serie, RequestAggregatedMeasureDataTransaction> message)
    {
        return await CreateMessageReceiver()
            .ReceiveAsync(message, CancellationToken.None);
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
