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
using Dapper;
using Energinet.DataHub.EDI.Application.Actors;
using Energinet.DataHub.EDI.Application.Configuration.Authentication;
using Energinet.DataHub.EDI.Application.Configuration.DataAccess;
using Energinet.DataHub.EDI.Application.IncomingMessages.RequestAggregatedMeasureData;
using Energinet.DataHub.EDI.Domain.Actors;
using Energinet.DataHub.EDI.Domain.Documents;
using Energinet.DataHub.EDI.Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages;
using Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages.Queues;
using Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages.RequestAggregatedMeasureData;
using Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.ValidationErrors;
using Energinet.DataHub.EDI.Infrastructure.Configuration.Authentication;
using Energinet.DataHub.EDI.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.Infrastructure.IncomingMessages.RequestAggregatedMeasureData;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;
using Serie = Energinet.DataHub.EDI.Application.IncomingMessages.RequestAggregatedMeasureData.Serie;

namespace Energinet.DataHub.EDI.IntegrationTests.Infrastructure.CimMessageAdapter.Messages.RequestAggregatedMeasureData;

[IntegrationTest]
public class RequestAggregatedMeasureDataReceiverTests : TestBase, IAsyncLifetime
{
    private readonly MessageParser _messageParser;
    private readonly IMarketActorAuthenticator _marketActorAuthenticator;
    private readonly ITransactionIdRepository _transactionIdRepository;
    private readonly IMessageIdRepository _messageIdRepository;
    private readonly ProcessTypeValidator _processTypeValidator;
    private readonly MessageTypeValidator _messageTypeValidator;
    private readonly CalculationResponsibleReceiverVerification _calculationResponsibleReceiverVerification;
    private readonly B2BContext _b2BContext;
    private readonly IAggregatedMeasureDataProcessRepository _aggregatedMeasureDataProcessRepository;
    private readonly List<Claim> _claims = new()
    {
        new(ClaimsMap.UserId, new CreateActorCommand(Guid.NewGuid().ToString(), SampleData.StsAssignedUserId, SampleData.SenderId).B2CId),
    };

    public RequestAggregatedMeasureDataReceiverTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _messageParser = GetService<MessageParser>();
        _transactionIdRepository = GetService<ITransactionIdRepository>();
        _messageIdRepository = GetService<IMessageIdRepository>();
        _marketActorAuthenticator = GetService<IMarketActorAuthenticator>();
        _processTypeValidator = GetService<ProcessTypeValidator>();
        _messageTypeValidator = GetService<MessageTypeValidator>();
        _calculationResponsibleReceiverVerification = GetService<CalculationResponsibleReceiverVerification>();
        _b2BContext = GetService<B2BContext>();
        _aggregatedMeasureDataProcessRepository = GetService<IAggregatedMeasureDataProcessRepository>();
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

        await InvokeCommandAsync(new CreateActorCommand(Guid.NewGuid().ToString(), SampleData.StsAssignedUserId, SampleData.SenderId)).ConfigureAwait(false);
        await InvokeCommandAsync(new CreateActorCommand(Guid.NewGuid().ToString(), SampleData.SecondStsAssignedUserId, SampleData.SecondSenderId)).ConfigureAwait(false);
        //TODO: Consider removing authentication from validation (message receiver).
        await _marketActorAuthenticator.AuthenticateAsync(new ClaimsPrincipal(new ClaimsIdentity(_claims)), CancellationToken.None).ConfigureAwait(false);
    }

    public Task DisposeAsync()
    {
        _b2BContext.Dispose();
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
        var marketDocument = RequestAggregatedMeasureDocumentFactory.Created(messageParserResult.IncomingMarketDocument!);
        var result = await CreateMessageReceiver().ValidateAsync(marketDocument, CancellationToken.None).ConfigureAwait(false);

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
        var marketDocument = RequestAggregatedMeasureDocumentFactory.Created(messageParserResult.IncomingMarketDocument!);
        var result = await CreateMessageReceiver().ValidateAsync(marketDocument, CancellationToken.None).ConfigureAwait(false);

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
        var marketDocument = RequestAggregatedMeasureDocumentFactory.Created(messageParserResult.IncomingMarketDocument!);
        var result = await CreateMessageReceiver().ValidateAsync(marketDocument, CancellationToken.None).ConfigureAwait(false);

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
        var marketDocument = RequestAggregatedMeasureDocumentFactory.Created(messageParserResult.IncomingMarketDocument!);
        var result = await CreateMessageReceiver().ValidateAsync(marketDocument, CancellationToken.None).ConfigureAwait(false);

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
        var marketDocument = RequestAggregatedMeasureDocumentFactory.Created(messageParserResult.IncomingMarketDocument!);
        var result = await CreateMessageReceiver().ValidateAsync(marketDocument, CancellationToken.None).ConfigureAwait(false);

        Assert.Contains(result.Errors, error => error is AuthenticatedUserDoesNotMatchSenderId);
    }

    [Fact]
    public async Task Authenticated_user_must_hold_the_role_type_as_specified_in_message()
    {
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .Message();

        var messageParserResult = await ParseMessageAsync(message).ConfigureAwait(false);
        var marketDocument = RequestAggregatedMeasureDocumentFactory.Created(messageParserResult.IncomingMarketDocument!);
        var result = await CreateMessageReceiver().ValidateAsync(marketDocument, CancellationToken.None).ConfigureAwait(false);

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
            .DuplicateSeriesRecords()
            .Message();

        var messageParserResult = await ParseMessageAsync(message).ConfigureAwait(false);
        var marketDocument = RequestAggregatedMeasureDocumentFactory.Created(messageParserResult.IncomingMarketDocument!);
        var result = await CreateMessageReceiver().ValidateAsync(marketDocument, CancellationToken.None).ConfigureAwait(false);

        Assert.Contains(result.Errors, error => error is DuplicateTransactionIdDetected);
    }

    [Fact]
    public async Task Series_can_have_same_transaction_ids_across_senders()
    {
        var knownReceiverId = "5790001330552";
        var knownReceiverRole = "DDZ";
        await CreateIdentityWithRoles(new List<MarketRole> { MarketRole.EnergySupplier })
            .ConfigureAwait(false);
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithReceiverRole(knownReceiverRole)
            .WithReceiverId(knownReceiverId)
            .Message();

        var messageParserResult = await ParseMessageAsync(message).ConfigureAwait(false);
        var marketDocument = RequestAggregatedMeasureDocumentFactory.Created(messageParserResult.IncomingMarketDocument!);
        var resultFromFirstMessage = await CreateMessageReceiver().ValidateAsync(marketDocument, CancellationToken.None).ConfigureAwait(false);

        // Request from a second sender.
        await using var message02 = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithReceiverRole(knownReceiverRole)
            .WithReceiverId(knownReceiverId)
            .WithMessageId("123564789123564789123564789123564789")
            .Message();

        await CreateSecondIdentityWithRoles(new List<MarketRole> { MarketRole.EnergySupplier }, SampleData.SecondSenderId, SampleData.SecondStsAssignedUserId)
            .ConfigureAwait(false);
        var messageParserResult2 = await ParseMessageAsync(message02).ConfigureAwait(false);
        var marketDocument2 = RequestAggregatedMeasureDocumentFactory.Created(messageParserResult2.IncomingMarketDocument!);
        var resultFromSecondMessage = await CreateMessageReceiver().ValidateAsync(marketDocument2, CancellationToken.None).ConfigureAwait(false);

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
            .WithSeriesTransactionId(string.Empty)
            .Message();

        var messageParserResult = await ParseMessageAsync(message).ConfigureAwait(false);
        var marketDocument = RequestAggregatedMeasureDocumentFactory.Created(messageParserResult.IncomingMarketDocument!);
        var result = await CreateMessageReceiver().ValidateAsync(marketDocument, CancellationToken.None).ConfigureAwait(false);

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
        var marketDocument = RequestAggregatedMeasureDocumentFactory.Created(messageParserResult.IncomingMarketDocument!);
        var result = await CreateMessageReceiver().ValidateAsync(marketDocument, CancellationToken.None).ConfigureAwait(false);

        Assert.Contains(result.Errors, error => error is EmptyMessageId);
    }

    [Fact]
    public async Task Message_id_may_be_reused_across_senders()
    {
        await CreateIdentityWithRoles(new List<MarketRole> { MarketRole.EnergySupplier })
            .ConfigureAwait(false);
        await using var message01 = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .Message();
        await using var message02 = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithSenderId("1212121212121")
            .Message();

        var messageParserResult01 = await ParseMessageAsync(message01).ConfigureAwait(false);
        var marketDocument = RequestAggregatedMeasureDocumentFactory.Created(messageParserResult01.IncomingMarketDocument!);
        var result01 = await CreateMessageReceiver().ValidateAsync(marketDocument, CancellationToken.None).ConfigureAwait(false);

        await CreateSecondIdentityWithRoles(new List<MarketRole> { MarketRole.EnergySupplier }, SampleData.SecondSenderId, SampleData.SecondStsAssignedUserId)
            .ConfigureAwait(false);
        var messageParserResult02 = await ParseMessageAsync(message02).ConfigureAwait(false);
        var marketDocument02 = RequestAggregatedMeasureDocumentFactory.Created(messageParserResult02.IncomingMarketDocument!);
        var result02 = await CreateMessageReceiver().ValidateAsync(marketDocument02, CancellationToken.None).ConfigureAwait(false);

        Assert.DoesNotContain(result01.Errors, error => error is DuplicateMessageIdDetected);
        Assert.DoesNotContain(result02.Errors, error => error is DuplicateMessageIdDetected);
    }

    [Fact]
    public async Task Message_ids_must_be_unique_for_sender()
    {
        await CreateIdentityWithRoles(new List<MarketRole> { MarketRole.EnergySupplier })
            .ConfigureAwait(false);
        await using var message01 = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithReceiverId("5790001330552")
            .Message();

        var messageParserResult01 = await ParseMessageAsync(message01).ConfigureAwait(false);
        var marketDocument = RequestAggregatedMeasureDocumentFactory.Created(messageParserResult01.IncomingMarketDocument!);
        var result01 = await CreateMessageReceiver().ValidateAsync(marketDocument, CancellationToken.None).ConfigureAwait(false);
        var result02 = await CreateMessageReceiver().ValidateAsync(marketDocument, CancellationToken.None).ConfigureAwait(false);

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

        var messageParserResult = await ParseMessageAsync(message).ConfigureAwait(false);
        var marketDocument = RequestAggregatedMeasureDocumentFactory.Created(messageParserResult.IncomingMarketDocument!);
        var result = await CreateMessageReceiver().ValidateAsync(marketDocument, CancellationToken.None).ConfigureAwait(false);

        Assert.DoesNotContain(result.Errors, error => error is SenderRoleTypeIsNotAuthorized);
    }

    [Fact]
    public async Task Return_failure_if_xml_schema_for_business_reason_does_not_exist()
    {
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData("Infrastructure.CimMessageAdapter//Messages//Xml//BadRequestAggregatedMeasureData.xml")
            .Message();

        var messageParserResult = await ParseMessageAsync(message).ConfigureAwait(false);
        var marketDocument = RequestAggregatedMeasureDocumentFactory.Created(messageParserResult.IncomingMarketDocument!);
        var result = await CreateMessageReceiver().ValidateAsync(marketDocument, CancellationToken.None).ConfigureAwait(false);

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
        var marketDocument = RequestAggregatedMeasureDocumentFactory.Created(messageParserResult.IncomingMarketDocument!);
        var result = await CreateMessageReceiver().ValidateAsync(marketDocument, CancellationToken.None).ConfigureAwait(false);

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
            .Message();

        var messageParserResult = await ParseMessageAsync(message).ConfigureAwait(false);

        Assert.False(messageParserResult.Success);
        Assert.Contains(messageParserResult.Errors, error => error is InvalidMessageStructure);
        Assert.Contains(messageParserResult.Errors, error => error.Message.Contains(new NotSupportedMessageType(string.Empty).Target!, StringComparison.InvariantCultureIgnoreCase));
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
            .WithReceiverRole(knownReceiverRole)
            .WithReceiverId(knownReceiverId)
            .Message();

        var messageParserResult = await ParseMessageAsync(message).ConfigureAwait(false);
        var marketDocument = RequestAggregatedMeasureDocumentFactory.Created(messageParserResult.IncomingMarketDocument!);
        var result = await CreateMessageReceiver().ValidateAsync(marketDocument, CancellationToken.None).ConfigureAwait(false);

        Assert.Contains(result.Errors, error => error is InvalidMessageIdSize);
    }

    [Fact]
    public async Task Valid_activity_records_are_extracted_and_committed_as_a_process()
    {
        await CreateIdentityWithRoles(new List<MarketRole> { MarketRole.EnergySupplier })
            .ConfigureAwait(false);
        var knownReceiverId = "5790001330552";
        var knownReceiverRole = "DGL";
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithReceiverRole(knownReceiverRole)
            .WithReceiverId(knownReceiverId)
            .Message();

        var messageParserResult = await ParseMessageAsync(message).ConfigureAwait(false);
        var result = await CreateInitializeRequestAggregatedMeasureProcessesHandler()
            .Handle(new InitializeAggregatedMeasureDataProcessesCommand(messageParserResult), CancellationToken.None);

        var process = _b2BContext.AggregatedMeasureDataProcesses.Local.FirstOrDefault();
        Assert.True(result.Success);
        Assert.NotNull(process);

        var document = messageParserResult!.IncomingMarketDocument!;
        await AssertTransactionIdIsStoredAsync(document.Header.SenderId, document.MarketActivityRecords.First().Id).ConfigureAwait(false);
        await AssertMessageIdIsStoredAsync(document.Header.SenderId, document.Header.MessageId).ConfigureAwait(false);
    }

    [Fact]
    public async Task Multiple_activity_records_are_committed_as_processes()
    {
        await CreateIdentityWithRoles(new List<MarketRole> { MarketRole.EnergySupplier })
            .ConfigureAwait(false);
        var knownReceiverId = "5790001330552";
        var knownReceiverRole = "DGL";
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .DuplicateSeriesRecords()
            .WithSeriesTransactionId(Guid.NewGuid().ToString())
            .WithReceiverRole(knownReceiverRole)
            .WithReceiverId(knownReceiverId)
            .Message();

        var messageParserResult = await ParseMessageAsync(message).ConfigureAwait(false);
        var result = await CreateInitializeRequestAggregatedMeasureProcessesHandler()
            .Handle(new InitializeAggregatedMeasureDataProcessesCommand(messageParserResult), CancellationToken.None);

        var processes = _b2BContext.AggregatedMeasureDataProcesses.Local.ToList();
        Assert.True(result.Success);
        Assert.NotNull(processes);
        Assert.Equal(messageParserResult.IncomingMarketDocument!.MarketActivityRecords.Count, processes.Count);

        var document = messageParserResult!.IncomingMarketDocument!;
        await AssertTransactionIdIsStoredAsync(document.Header.SenderId, document.MarketActivityRecords.First().Id).ConfigureAwait(false);
        await AssertTransactionIdIsStoredAsync(document.Header.SenderId, document.MarketActivityRecords.Last().Id).ConfigureAwait(false);
        await AssertMessageIdIsStoredAsync(document.Header.SenderId, document.Header.MessageId).ConfigureAwait(false);
    }

    [Fact]
    public async Task Transaction_ids_are_unique_across_scopes()
    {
        await CreateIdentityWithRoles(new List<MarketRole> { MarketRole.EnergySupplier })
            .ConfigureAwait(false);
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithReceiverId("5790001330552")
            .WithReceiverRole("DGL")
            .Message();

        var messageParserResult = await ParseMessageAsync(message).ConfigureAwait(false);

        // Act
        var marketDocument = RequestAggregatedMeasureDocumentFactory.Created(messageParserResult.IncomingMarketDocument!);
        var receivedResult1 = CreateMessageReceiver().ValidateAsync(marketDocument, CancellationToken.None);
        var receivedResult2 = CreateMessageReceiver().ValidateAsync(marketDocument, CancellationToken.None);

        await Task.WhenAll(receivedResult1, receivedResult2).ConfigureAwait(false);

        // Assert
        Assert.NotNull(receivedResult1);
        Assert.NotNull(receivedResult2);

        var result1 = await receivedResult1;
        var result2 = await receivedResult2;
        if (result1.Success)
        {
            Assert.False(result2.Success);
        }
        else
        {
            Assert.True(result2.Success);
        }

        var document = messageParserResult!.IncomingMarketDocument!;
        await AssertTransactionIdIsStoredAsync(document.Header.SenderId, document.MarketActivityRecords.First().Id).ConfigureAwait(false);
        await AssertMessageIdIsStoredAsync(document.Header.SenderId, document.Header.MessageId).ConfigureAwait(false);
    }

    [Fact]
    public async Task Transaction_id_and_message_id_are_not_registered_when_duplicated_across_scopes()
    {
        await CreateIdentityWithRoles(new List<MarketRole> { MarketRole.EnergySupplier })
            .ConfigureAwait(false);
        await using var message01 = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithReceiverId("5790001330552")
            .WithReceiverRole("DGL")
            .Message();
        await using var message02 = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithReceiverId("5790001330552")
            .WithReceiverRole("DGL")
            .WithSeriesTransactionId("123564789123564789523564789123564789")
            .Message();

        var messageParserResult01 = await ParseMessageAsync(message01).ConfigureAwait(false);
        var marketDocument01 = RequestAggregatedMeasureDocumentFactory.Created(messageParserResult01.IncomingMarketDocument!);
        var messageParserResult02 = await ParseMessageAsync(message02).ConfigureAwait(false);
        var marketDocument02 = RequestAggregatedMeasureDocumentFactory.Created(messageParserResult02.IncomingMarketDocument!);

        // Act
        var receivedResult1 = CreateMessageReceiver().ValidateAsync(marketDocument01, CancellationToken.None);
        var receivedResult2 = CreateMessageReceiver().ValidateAsync(marketDocument02, CancellationToken.None);

        await Task.WhenAll(receivedResult1, receivedResult2).ConfigureAwait(false);

        // Assert
        Assert.NotNull(receivedResult1);
        Assert.NotNull(receivedResult2);

        var firstSucceeded = false;
        var result1 = await receivedResult1;
        var result2 = await receivedResult2;

        if (result1.Success)
        {
            firstSucceeded = true;
            Assert.False(result2.Success);
        }
        else
        {
            Assert.True(result2.Success);
        }

        var document01 = messageParserResult01!.IncomingMarketDocument!;
        var document02 = messageParserResult02!.IncomingMarketDocument!;

        var successfulDocument = firstSucceeded
            ? document01
            : document02;
        var unsuccessfulDocument = firstSucceeded
            ? document02
            : document01;

        await AssertTransactionIdIsStoredAsync(successfulDocument.Header.SenderId, successfulDocument.MarketActivityRecords.First().Id).ConfigureAwait(false);
        await AssertMessageIdIsStoredAsync(successfulDocument.Header.SenderId, successfulDocument.Header.MessageId).ConfigureAwait(false);

        await AssertTransactionIdIsNotStoredAsync(unsuccessfulDocument.Header.SenderId, unsuccessfulDocument.MarketActivityRecords.First().Id).ConfigureAwait(false);
    }

    [Fact]
    public async Task Transaction_and_message_ids_are_not_saved_when_receiving_a_faulted_request()
    {
        await CreateIdentityWithRoles(new List<MarketRole> { MarketRole.EnergySupplier })
            .ConfigureAwait(false);
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithReceiverId("5790001330552") // This is not MDR
            .WithReceiverRole("MDR")
            .Message();

        var messageParserResult = await ParseMessageAsync(message).ConfigureAwait(false);
        var marketDocument = RequestAggregatedMeasureDocumentFactory.Created(messageParserResult.IncomingMarketDocument!);

        // Act
        var result = await CreateMessageReceiver().ValidateAsync(marketDocument, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);

        var document = messageParserResult!.IncomingMarketDocument!;
        await AssertTransactionIdIsNotStoredAsync(document.Header.SenderId, document.MarketActivityRecords.First().Id).ConfigureAwait(false);
        await AssertMessageIdIsNotStoredAsync(document.Header.SenderId, document.Header.MessageId).ConfigureAwait(false);
    }

    [Fact]
    public async Task Transaction_id_must_not_be_less_than_36_characters()
    {
        await CreateIdentityWithRoles(new List<MarketRole> { MarketRole.EnergySupplier })
            .ConfigureAwait(false);
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithSeriesTransactionId("12356478912356478912356478912356478")
            .Message();

        var messageParserResult = await ParseMessageAsync(message).ConfigureAwait(false);
        var marketDocument = RequestAggregatedMeasureDocumentFactory.Created(messageParserResult.IncomingMarketDocument!);
        var result = await CreateMessageReceiver().ValidateAsync(marketDocument, CancellationToken.None).ConfigureAwait(false);

        Assert.Contains(result.Errors, error => error is InvalidTransactionIdSize);
    }

    [Fact]
    public async Task Transaction_id_must_be_36_characters()
    {
        await CreateIdentityWithRoles(new List<MarketRole> { MarketRole.EnergySupplier })
            .ConfigureAwait(false);
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithSeriesTransactionId("123564789123564789123564789123564789")
            .Message();

        var messageParserResult = await ParseMessageAsync(message).ConfigureAwait(false);
        var marketDocument = RequestAggregatedMeasureDocumentFactory.Created(messageParserResult.IncomingMarketDocument!);
        var result = await CreateMessageReceiver().ValidateAsync(marketDocument, CancellationToken.None).ConfigureAwait(false);

        Assert.DoesNotContain(result.Errors, error => error is InvalidTransactionIdSize);
    }

    [Fact]
    public async Task Transaction_id_must_not_be_more_than_36_characters()
    {
        await CreateIdentityWithRoles(new List<MarketRole> { MarketRole.EnergySupplier })
            .ConfigureAwait(false);
        await using var message = BusinessMessageBuilder
            .RequestAggregatedMeasureData()
            .WithSeriesTransactionId("123564789123564789123564789123564789_123564789123564789123564789123564789")
            .Message();

        var messageParserResult = await ParseMessageAsync(message).ConfigureAwait(false);
        var marketDocument = RequestAggregatedMeasureDocumentFactory.Created(messageParserResult.IncomingMarketDocument!);
        var result = await CreateMessageReceiver().ValidateAsync(marketDocument, CancellationToken.None).ConfigureAwait(false);

        Assert.Contains(result.Errors, error => error is InvalidTransactionIdSize);
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
                new CreateActorCommand(Guid.NewGuid().ToString(), b2cId, senderId).B2CId),
        };
        claims.AddRange(roles.Select(ClaimsMap.RoleFrom));
        await _marketActorAuthenticator
            .AuthenticateAsync(new ClaimsPrincipal(new ClaimsIdentity(claims)), CancellationToken.None)
            .ConfigureAwait(false);
    }

    private InitializeAggregatedMeasureDataProcessesHandler CreateInitializeRequestAggregatedMeasureProcessesHandler()
    {
        return new InitializeAggregatedMeasureDataProcessesHandler(CreateMessageReceiver(), _aggregatedMeasureDataProcessRepository);
    }

    private RequestAggregatedMeasureDataValidator CreateMessageReceiver()
    {
        var messageReceiver = new RequestAggregatedMeasureDataValidator(
            _messageIdRepository,
            _transactionIdRepository,
            new SenderAuthorizer(_marketActorAuthenticator),
            _processTypeValidator,
            _messageTypeValidator,
            _calculationResponsibleReceiverVerification);
        return messageReceiver;
    }

    private Task<MessageParserResult<Serie, RequestAggregatedMeasureDataTransactionCommand>> ParseMessageAsync(Stream message)
    {
        return _messageParser.ParseAsync(message, DocumentFormat.Xml, CancellationToken.None);
    }

    private async Task AssertTransactionIdIsStoredAsync(string senderId, string transactionId)
    {
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None).ConfigureAwait(false);
        var sql =
            "SELECT * FROM dbo.TransactionRegistry WHERE TransactionId = @TransactionId AND SenderId = @SenderId";
        var transaction = await connection.QueryFirstOrDefaultAsync(sql, new { TransactionId = transactionId, SenderId = senderId }).ConfigureAwait(false);
        Assert.NotNull(transaction);
    }

    private async Task AssertTransactionIdIsNotStoredAsync(string senderId, string transactionId)
    {
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None).ConfigureAwait(false);
        var sql =
            "SELECT * FROM dbo.TransactionRegistry WHERE TransactionId = @TransactionId AND SenderId = @SenderId";
        var transaction = await connection.QueryFirstOrDefaultAsync(sql, new { TransactionId = transactionId, SenderId = senderId }).ConfigureAwait(false);
        Assert.Null(transaction);
    }

    private async Task AssertMessageIdIsStoredAsync(string senderId, string messageId)
    {
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None).ConfigureAwait(false);
        var sql =
            "SELECT * FROM dbo.MessageRegistry WHERE MessageId = @MessageId AND SenderId = @SenderId";
        var message = await connection.QueryFirstOrDefaultAsync(sql, new { MessageId = messageId, SenderId = senderId }).ConfigureAwait(false);
        Assert.NotNull(message);
    }

    private async Task AssertMessageIdIsNotStoredAsync(string senderId, string messageId)
    {
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None).ConfigureAwait(false);
        var sql =
            "SELECT * FROM dbo.MessageRegistry WHERE MessageId = @MessageId AND SenderId = @SenderId";
        var message = await connection.QueryFirstOrDefaultAsync(sql, new { MessageId = messageId, SenderId = senderId }).ConfigureAwait(false);
        Assert.Null(message);
    }
}
