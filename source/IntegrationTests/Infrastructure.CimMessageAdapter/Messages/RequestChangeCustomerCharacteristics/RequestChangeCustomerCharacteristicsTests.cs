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
using Energinet.DataHub.EDI.Application.Actors;
using Energinet.DataHub.EDI.Application.Configuration.Authentication;
using Energinet.DataHub.EDI.Application.IncomingMessages.RequestChangeCustomerCharacteristics;
using Energinet.DataHub.EDI.Domain.Actors;
using Energinet.DataHub.EDI.Domain.Documents;
using Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages;
using Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages.RequestChangeCustomerCharacteristics;
using Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.ValidationErrors;
using Energinet.DataHub.EDI.Infrastructure.Configuration.Authentication;
using Energinet.DataHub.EDI.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;
using MessageParser = Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages.RequestChangeCustomerCharacteristics.MessageParser;
using Result = Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages.Result;
using SenderAuthorizer = Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages.RequestChangeCustomerCharacteristics.SenderAuthorizer;

namespace Energinet.DataHub.EDI.IntegrationTests.Infrastructure.CimMessageAdapter.Messages.RequestChangeCustomerCharacteristics;

[IntegrationTest]
public class RequestChangeCustomerCharacteristicsTests : TestBase, IAsyncLifetime
{
    private readonly MessageParser _messageParser;
    private readonly IMarketActorAuthenticator _marketActorAuthenticator;
    private readonly ITransactionIdRepository _transactionIdRepository;
    private readonly IMessageIdRepository _messageIdRepository;
    private readonly DefaultProcessTypeValidator _processTypeValidator;
    private readonly DefaultMessageTypeValidator _messageTypeValidator;
    private readonly MasterDataReceiverResponsibleVerification _masterDataReceiverResponsibleVerification;
    private readonly B2BContext _b2BContext;
    private List<Claim> _claims = new();

    public RequestChangeCustomerCharacteristicsTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _messageParser = GetService<MessageParser>();
        _transactionIdRepository = GetService<ITransactionIdRepository>();
        _messageIdRepository = GetService<IMessageIdRepository>();
        _marketActorAuthenticator = GetService<IMarketActorAuthenticator>();
        _processTypeValidator = GetService<DefaultProcessTypeValidator>();
        _messageTypeValidator = GetService<DefaultMessageTypeValidator>();
        _masterDataReceiverResponsibleVerification = GetService<MasterDataReceiverResponsibleVerification>();
        _b2BContext = GetService<B2BContext>();
    }

    public async Task InitializeAsync()
    {
#pragma warning disable CA2007
        var createActorCommand =
            new CreateActorCommand(Guid.NewGuid().ToString(), SampleData.StsAssignedUserId, SampleData.ActorNumber);
        await InvokeCommandAsync(createActorCommand).ConfigureAwait(false);

        _claims = new List<Claim>()
        {
            new(ClaimsMap.UserId, SampleData.StsAssignedUserId),
            ClaimsMap.RoleFrom(MarketRole.EnergySupplier),
        };

        await _marketActorAuthenticator.AuthenticateAsync(CreateIdentity(), CancellationToken.None);
    }

    public Task DisposeAsync()
    {
        _b2BContext.Dispose();
        return Task.CompletedTask;
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

        Assert.Contains(result.Errors, error => error is InvalidReceiverId);
    }

    [Fact]
    public async Task Receiver_role_must_be_metering_point_administrator()
    {
        await using var message = BusinessMessageBuilder
            .RequestChangeCustomerCharacteristics()
            .WithReceiverRole("DDK")
            .Message();

        var result = await ReceiveRequestChangeCustomerCharacteristicsMessage(message).ConfigureAwait(false);

        Assert.Contains(result.Errors, error => error is InvalidReceiverRole);
    }

    [Fact]
    public async Task Sender_role_type_must_be_the_role_of_energy_supplier()
    {
        await using var message = BusinessMessageBuilder
            .RequestChangeCustomerCharacteristics()
            .WithSenderRole("DDK")
            .Message();

        var result = await ReceiveRequestChangeCustomerCharacteristicsMessage(message).ConfigureAwait(false);

        Assert.Contains(result.Errors, error => error is SenderRoleTypeIsNotAuthorized);
    }

    [Fact]
    public async Task Authenticated_user_must_hold_the_role_type_as_specified_in_message()
    {
        await _marketActorAuthenticator.AuthenticateAsync(CreateIdentityWithoutRoles(), CancellationToken.None);
        await using var message = BusinessMessageBuilder
            .RequestChangeCustomerCharacteristics()
            .Message();

        var result = await ReceiveRequestChangeCustomerCharacteristicsMessage(message).ConfigureAwait(false);

        Assert.Contains(result.Errors, error => error is SenderRoleTypeIsNotAuthorized);
    }

    [Fact]
    public async Task Sender_id_must_match_the_organization_of_the_current_authenticated_user()
    {
        await using var message = BusinessMessageBuilder
            .RequestChangeCustomerCharacteristics()
            .Message();

        var result = await ReceiveRequestChangeCustomerCharacteristicsMessage(message).ConfigureAwait(false);

        Assert.Contains(result.Errors, error => error is AuthenticatedUserDoesNotMatchSenderId);
    }

    [Fact]
    public async Task Return_failure_if_xml_schema_for_business_reason_does_not_exist()
    {
        await using var message = BusinessMessageBuilder
            .RequestChangeCustomerCharacteristics("Infrastructure.CimMessageAdapter//Messages//Xml//BadRequestChangeCustomerCharacteristics.xml")
            .Message();

        var result = await ReceiveRequestChangeCustomerCharacteristicsMessage(message)
            .ConfigureAwait(false);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, error => error is InvalidBusinessReasonOrVersion);
    }

    // [Fact]
    // public async Task Valid_activity_records_are_extracted_and_committed_to_queue()
    // {
    //     await using var message = BusinessMessageBuilder
    //         .RequestChangeCustomerCharacteristics()
    //         .WithSenderId(SampleData.ActorNumber)
    //         .Message();
    //
    //     await ReceiveRequestChangeCustomerCharacteristicsMessage(message)
    //         .ConfigureAwait(false);
    //
    //     var process = _b2BContext.AggregatedMeasureDataProcesses.FirstOrDefault();
    //     Assert.NotNull(process);
    // }

    [Fact]
    public async Task Activity_records_are_not_committed_to_queue_if_any_message_header_values_are_invalid()
    {
        await SimulateDuplicationOfMessageIds(_messageIdRepository).ConfigureAwait(false);

        Assert.Empty(_b2BContext.AggregatedMeasureDataProcesses);
    }

    [Fact]
    public async Task Activity_records_must_have_unique_transaction_ids()
    {
        await using var message = BusinessMessageBuilder
            .RequestChangeCustomerCharacteristics()
            .WithSenderId(SampleData.ActorNumber)
            .DuplicateMarketActivityRecords()
            .Message();

        var result = await ReceiveRequestChangeCustomerCharacteristicsMessage(message)
            .ConfigureAwait(false);

        Assert.Contains(result.Errors, error => error is DuplicateTransactionIdDetected);
        Assert.Empty(_b2BContext.AggregatedMeasureDataProcesses);
    }

    private static ClaimsPrincipal CreateClaimsPrincipal(IEnumerable<Claim> claims)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(claims));
    }

    private async Task SimulateDuplicationOfMessageIds(IMessageIdRepository messageIdRepository)
    {
        var messageBuilder = BusinessMessageBuilder.RequestChangeCustomerCharacteristics();

        using var originalMessage = messageBuilder.Message();
        await CreateMessageReceiver(messageIdRepository).ReceiveAsync(await ParseMessageAsync(originalMessage).ConfigureAwait(false), CancellationToken.None)
            .ConfigureAwait(false);

        using var duplicateMessage = messageBuilder.Message();
        await CreateMessageReceiver(messageIdRepository).ReceiveAsync(await ParseMessageAsync(duplicateMessage).ConfigureAwait(false), CancellationToken.None)
            .ConfigureAwait(false);
    }

    private async Task<Result> ReceiveRequestChangeCustomerCharacteristicsMessage(Stream message)
    {
        return await CreateMessageReceiver()
            .ReceiveAsync(await ParseMessageAsync(message).ConfigureAwait(false), CancellationToken.None);
    }

    private Task<MessageParserResult<MarketActivityRecord, RequestChangeCustomerCharacteristicsTransaction>> ParseMessageAsync(Stream message)
    {
        return _messageParser.ParseAsync(message, DocumentFormat.Xml, CancellationToken.None);
    }

    private MessageReceiver<global::Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages.Queues.RequestChangeCustomerCharacteristicsTransaction> CreateMessageReceiver()
    {
        var messageReceiver = new RequestChangeCustomerCharacteristicsReceiver(
            _messageIdRepository,
            _transactionIdRepository,
            new SenderAuthorizer(_marketActorAuthenticator),
            _processTypeValidator,
            _messageTypeValidator,
            _masterDataReceiverResponsibleVerification);
        return messageReceiver;
    }

    private MessageReceiver<global::Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages.Queues.RequestChangeCustomerCharacteristicsTransaction> CreateMessageReceiver(IMessageIdRepository messageIdRepository)
    {
        var messageReceiver = new RequestChangeCustomerCharacteristicsReceiver(messageIdRepository, _transactionIdRepository, new SenderAuthorizer(_marketActorAuthenticator), _processTypeValidator, _messageTypeValidator, _masterDataReceiverResponsibleVerification);
        return messageReceiver;
    }

    private ClaimsPrincipal CreateIdentity()
    {
        return new ClaimsPrincipal(new ClaimsIdentity(_claims));
    }

    private ClaimsPrincipal CreateIdentityWithoutRoles()
    {
        var claims = _claims.ToList();
        claims.RemoveAll(claim => claim.Type.Equals(ClaimTypes.Role, StringComparison.OrdinalIgnoreCase));
        return CreateClaimsPrincipal(claims);
    }
}
