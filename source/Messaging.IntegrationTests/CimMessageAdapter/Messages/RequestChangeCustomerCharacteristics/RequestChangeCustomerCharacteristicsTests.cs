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
using System.Threading.Tasks;
using Messaging.Application.Configuration.Authentication;
using Messaging.Application.IncomingMessages.RequestChangeCustomerCharacteristics;
using Messaging.CimMessageAdapter.Messages;
using Messaging.CimMessageAdapter.Messages.RequestChangeCustomerCharacteristics;
using Messaging.Domain.OutgoingMessages;
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
            .RequestChangeCustomerCharacteristics()
            .WithReceiverRole("DDK")
            .Message();

        var result = await ReceiveRequestChangeCustomerCharacteristicsMessage(message).ConfigureAwait(false);

        AssertContainsError(result, "B2B-008");
    }

    [Fact]
    public async Task Sender_role_type_must_be_the_role_of_energy_supplier()
    {
        await using var message = BusinessMessageBuilder
            .RequestChangeCustomerCharacteristics()
            .WithSenderRole("DDK")
            .Message();

        var result = await ReceiveRequestChangeCustomerCharacteristicsMessage(message).ConfigureAwait(false);

        AssertContainsError(result, "B2B-008");
    }

    [Fact]
    public async Task Authenticated_user_must_hold_the_role_type_as_specified_in_message()
    {
        _marketActorAuthenticator.Authenticate(CreateIdentityWithoutRoles());
        await using var message = BusinessMessageBuilder
            .RequestChangeCustomerCharacteristics()
            .Message();

        var result = await ReceiveRequestChangeCustomerCharacteristicsMessage(message).ConfigureAwait(false);

        AssertContainsError(result, "B2B-008");
    }

    [Fact]
    public async Task Sender_id_must_match_the_organization_of_the_current_authenticated_user()
    {
        _marketActorAuthenticator.Authenticate(CreateIdentity("Unknown_actor_identifier"));
        await using var message = BusinessMessageBuilder
            .RequestChangeCustomerCharacteristics()
            .Message();

        var result = await ReceiveRequestChangeCustomerCharacteristicsMessage(message).ConfigureAwait(false);

        AssertContainsError(result, "B2B-008");
    }

    [Fact]
    public async Task Return_failure_if_xml_schema_for_business_process_type_does_not_exist()
    {
        await using var message = BusinessMessageBuilder
            .RequestChangeCustomerCharacteristics("CimMessageAdapter//Messages//Xml//BadRequestChangeCustomerCharacteristics.xml")
            .Message();

        var result = await ReceiveRequestChangeCustomerCharacteristicsMessage(message)
            .ConfigureAwait(false);

        Assert.False(result.Success);
        AssertContainsError(result, "B2B-001");
    }

    [Fact]
    public async Task Valid_activity_records_are_extracted_and_committed_to_queue()
    {
        await using var message = BusinessMessageBuilder
            .RequestChangeCustomerCharacteristics()
            .Message();

        await ReceiveRequestChangeCustomerCharacteristicsMessage(message)
            .ConfigureAwait(false);

        var transaction = _messageQueueDispatcherSpy.CommittedItems.FirstOrDefault();
        Assert.NotNull(transaction);
    }

    [Fact]
    public async Task Activity_records_are_not_committed_to_queue_if_any_message_header_values_are_invalid()
    {
        await SimulateDuplicationOfMessageIds(_messageIds).ConfigureAwait(false);

        Assert.Empty(_messageQueueDispatcherSpy.CommittedItems);
    }

    [Fact]
    public async Task Activity_records_must_have_unique_transaction_ids()
    {
        await using var message = BusinessMessageBuilder
            .RequestChangeCustomerCharacteristics()
            .DuplicateMarketActivityRecords()
            .Message();

        var result = await ReceiveRequestChangeCustomerCharacteristicsMessage(message)
            .ConfigureAwait(false);

        AssertContainsError(result, "B2B-005");
        Assert.Empty(_messageQueueDispatcherSpy.CommittedItems);
    }

    private static void AssertContainsError(Result result, string errorCode)
    {
        Assert.Contains(result.Errors, error => error.Code.Equals(errorCode, StringComparison.OrdinalIgnoreCase));
    }

    private static ClaimsPrincipal CreateClaimsPrincipal(IEnumerable<Claim> claims)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(claims));
    }

    private async Task SimulateDuplicationOfMessageIds(IMessageIds messageIds)
    {
        var messageBuilder = BusinessMessageBuilder.RequestChangeCustomerCharacteristics();

        using var originalMessage = messageBuilder.Message();
        await CreateMessageReceiver(messageIds).ReceiveAsync(await ParseMessageAsync(originalMessage).ConfigureAwait(false))
            .ConfigureAwait(false);

        using var duplicateMessage = messageBuilder.Message();
        await CreateMessageReceiver(messageIds).ReceiveAsync(await ParseMessageAsync(duplicateMessage).ConfigureAwait(false))
            .ConfigureAwait(false);
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
        var messageReceiver = new RequestChangeCustomerCharacteristicsReceiver(
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

    private ClaimsPrincipal CreateIdentity(string actorIdentifier)
    {
        var claims = _claims.ToList();
        claims.Remove(claims.Find(claim => claim.Type.Equals("actorid", StringComparison.OrdinalIgnoreCase))!);
        claims.Add(new Claim("actorid", actorIdentifier));
        return new ClaimsPrincipal(new ClaimsIdentity(claims));
    }

    private ClaimsPrincipal CreateIdentityWithoutRoles()
    {
        var claims = _claims.ToList();
        claims.RemoveAll(claim => claim.Type.Equals(ClaimTypes.Role, StringComparison.OrdinalIgnoreCase));
        return CreateClaimsPrincipal(claims);
    }
}
