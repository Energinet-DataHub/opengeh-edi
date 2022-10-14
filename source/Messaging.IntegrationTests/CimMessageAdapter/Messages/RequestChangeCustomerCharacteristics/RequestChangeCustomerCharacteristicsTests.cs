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
using System.Security.Claims;
using System.Threading.Tasks;
using Messaging.Application.Configuration.Authentication;
using Messaging.Application.IncomingMessages.RequestChangeCustomerCharacteristics;
using Messaging.CimMessageAdapter.Messages;
using Messaging.CimMessageAdapter.Messages.RequestChangeOfSupplier;
using Messaging.Domain.OutgoingMessages;
using Messaging.IntegrationTests.CimMessageAdapter.Messages.RequestChangeCustomerCharcteristics;
using Messaging.IntegrationTests.CimMessageAdapter.Stubs;
using Messaging.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;
using MessageParser = Messaging.CimMessageAdapter.Messages.RequestChangeCustomerCharacteristics.MessageParser;

namespace Messaging.IntegrationTests.CimMessageAdapter.Messages.RequestChangeCustomerCharacteristics;

[IntegrationTest]
public class RequestChangeCustomerCharacteristicsTests : TestBase
{
    private readonly List<Claim> _claims = new List<Claim>()
    {
        new("azp", Guid.NewGuid().ToString()),
        new("actorid", "5799999933318"),
        new("actoridtype", "GLN"),
        new(ClaimTypes.Role, "balanceresponsibleparty"),
        new(ClaimTypes.Role, "electricalsupplier"),
    };

    private readonly MessageParser _messageParser;
    private readonly IMarketActorAuthenticator _marketActorAuthenticator;
    private readonly ITransactionIds _transactionIds;
    private readonly IMessageIds _messageIds;
    private MessageQueueDispatcherStub _messageQueueDispatcherSpy = new();

    public RequestChangeCustomerCharacteristicsTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _messageParser = GetService<MessageParser>();
        _transactionIds = GetService<ITransactionIds>();
        _messageIds = GetService<IMessageIds>();
        _marketActorAuthenticator = GetService<IMarketActorAuthenticator>();
        _marketActorAuthenticator.Authenticate(CreateIdentity());
    }

    [Fact]
    public async Task Receiver_id_must_be_known()
    {
        var unknownReceiverId = "5790001330550";
        await using var message = BusinessMessageBuilder
            .RequestChangeCustomerCharacteristics()
            .WithReceiverId(unknownReceiverId)
            .Message();

        var result = await ReceiveRequestChangeCustomerCharacteristicsMessage(message).ConfigureAwait(false);

        AssertContainsError(result, "B2B-008");
    }

    [Fact]
    public async Task Receiver_role_must_be_metering_point_administrator()
    {
        await using var message = BusinessMessageBuilder
            .RequestChangeOfSupplier()
            .WithReceiverRole("DDK")
            .Message();

        var result = await ReceiveRequestChangeCustomerCharacteristicsMessage(message).ConfigureAwait(false);

        AssertContainsError(result, "B2B-008");
    }

    private static void AssertContainsError(Result result, string errorCode)
    {
        Assert.Contains(result.Errors, error => error.Code.Equals(errorCode, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<Result> ReceiveRequestChangeCustomerCharacteristicsMessage(Stream message)
    {
        return await CreateMessageReceiver()
            .ReceiveAsync(await ParseMessageAsync(message).ConfigureAwait(false));
    }

    private Task<MessageParserResult<MarketActivityRecord, RequestChangeCustomerCharacteristicsTransaction>> ParseMessageAsync(Stream message)
    {
        return _messageParser.ParseAsync(message, CimFormat.Xml);
    }

    private MessageReceiver CreateMessageReceiver()
    {
        _messageQueueDispatcherSpy = new MessageQueueDispatcherStub();
        var messageReceiver = new RequestChangeOfSupplierReceiver(
            _messageIds,
            _messageQueueDispatcherSpy,
            _transactionIds,
            new SenderAuthorizer(_marketActorAuthenticator));
        return messageReceiver;
    }

    private MessageReceiver CreateMessageReceiver(IMessageIds messageIds)
    {
        _messageQueueDispatcherSpy = new MessageQueueDispatcherStub();
        var messageReceiver = new RequestChangeCustomerCharacteristicsReceiver(messageIds, _messageQueueDispatcherSpy, _transactionIds, new SenderAuthorizer(_marketActorAuthenticator));
        return messageReceiver;
    }

    private ClaimsPrincipal CreateIdentity()
    {
        return new ClaimsPrincipal(new ClaimsIdentity(_claims));
    }
}
