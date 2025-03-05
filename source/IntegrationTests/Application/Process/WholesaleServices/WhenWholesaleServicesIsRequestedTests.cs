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

using Azure.Messaging.ServiceBus;
using Dapper;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Tests.TestDoubles;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.Process.Application.Transactions.WholesaleServices;
using Energinet.DataHub.EDI.Process.Domain.Transactions.WholesaleServices;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.Edi.Requests;
using Energinet.EDI.DataHub.BuildingBlocks.Tests;
using FluentAssertions;
using FluentAssertions.Equivalency;
using FluentAssertions.Execution;
using Microsoft.Extensions.Azure;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.Process.WholesaleServices;

public class WhenWholesaleServicesIsRequestedTests : TestBase
{
    private readonly ProcessContext _processContext;
    private readonly ServiceBusSenderFactoryStub _serviceBusClientSenderFactory;
#pragma warning disable CA2213 // Disposable fields should be disposed
    private readonly ServiceBusSenderSpy _senderSpy;
#pragma warning restore CA2213 // Disposable fields should be disposed

    public WhenWholesaleServicesIsRequestedTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
        _processContext = GetService<ProcessContext>();
        _serviceBusClientSenderFactory = (ServiceBusSenderFactoryStub)GetService<IAzureClientFactory<ServiceBusSender>>();
        _senderSpy = new ServiceBusSenderSpy("Fake");
        _serviceBusClientSenderFactory.AddSenderSpy(_senderSpy);
    }

    [Fact]
    public async Task When_InitializeWholesaleServicesDto_is_received_ShouldInitializeProcess()
    {
        // Arrange
        var marketMessage = InitializeProcessDtoBuilder()
            .Build();

        // Act
        await InvokeCommandAsync(new InitializeWholesaleServicesProcessesCommand(marketMessage));

        // Assert
        var process = GetProcess(marketMessage.Series.Single().RequestedByActor.ActorNumber);
        process.Should().NotBeNull();
        process!.BusinessTransactionId.Value.Should().Be(marketMessage.Series.First().Id);
        process.Should().BeEquivalentTo(marketMessage, opt => opt.Using(new ProcessAndRequestComparer()));
        await AssertProcessState(marketMessage.MessageId, WholesaleServicesProcess.State.Initialized);
    }

    [Fact]
    public async Task When_WholesaleServicesProcess_is_initialized_service_bus_message_is_sent_to_wholesale()
    {
        // Arrange
        var exceptedServiceBusMessageSubject = nameof(WholesaleServicesRequest);
        var marketMessage = InitializeProcessDtoBuilder()
            .Build();

        // Act
        await InvokeCommandAsync(new InitializeWholesaleServicesProcessesCommand(marketMessage));
        await ProcessInternalCommandsAsync();

        // Assert
        var process = GetProcess(marketMessage.Series.Single().RequestedByActor.ActorNumber);
        var message = _senderSpy.LatestMessage;
        message.Should().NotBeNull();
        message!.Subject.Should().Be(exceptedServiceBusMessageSubject);
        process.Should().BeEquivalentTo(marketMessage, opt => opt.Using(new ProcessAndRequestComparer()));
        await AssertProcessState(marketMessage.MessageId, WholesaleServicesProcess.State.Sent);
    }

    [Fact]
    public async Task When_WholesaleServicesProcess_is_initialized_with_a_unused_value_process_can_still_be_handled()
    {
        // Arrange
        const string unusedBusinessReason = "A47";
        const string unusedSettlementVersion = "D10";

        var builder = InitializeProcessDtoBuilder()
            .SetBusinessReason(unusedBusinessReason)
            .SetSettlementVersion(unusedSettlementVersion);

        var marketMessage = builder.Build();

        // Act
        await InvokeCommandAsync(new InitializeWholesaleServicesProcessesCommand(marketMessage));
        await ProcessInternalCommandsAsync();

        // Assert
        using var assertionScope = new AssertionScope();

        _senderSpy.MessageSent.Should().BeTrue();

        var process = GetProcess(marketMessage.Series.Single().RequestedByActor.ActorNumber);
        process.Should().NotBeNull();
        await AssertProcessState(marketMessage.MessageId, WholesaleServicesProcess.State.Sent);

        process!.BusinessReason.IsUnused.Should().BeTrue();
        process.BusinessReason.Code.Should().Be(unusedBusinessReason);
        process.BusinessReason.Name.Should().Be(unusedBusinessReason);
        process.SettlementVersion!.IsUnused.Should().BeTrue();
        process.SettlementVersion.Code.Should().Be(unusedSettlementVersion);
        process.SettlementVersion.Name.Should().Be(unusedSettlementVersion);
    }

    [Fact]
    public async Task When_WholesaleServicesRequest_is_sent_to_wholesale_it_contains_no_CIM_codes()
    {
        // Arrange
        var marketMessage = InitializeProcessDtoBuilder()
            .Build();

        // Act
        await InvokeCommandAsync(new InitializeWholesaleServicesProcessesCommand(marketMessage));
        await ProcessInternalCommandsAsync();

        // Assert
        var message = _senderSpy.LatestMessage;

        using var scope = new AssertionScope();
        message.Should().NotBeNull();
        var wholesaleServicesRequest = WholesaleServicesRequest.Parser.ParseFrom(message!.Body);

        wholesaleServicesRequest.RequestedForActorRole.Should().NotBeCimCode();
        wholesaleServicesRequest.BusinessReason.Should().NotBeCimCode();
        wholesaleServicesRequest.Resolution.Should().NotBeCimCode();
        wholesaleServicesRequest.SettlementVersion.Should().NotBeCimCode();
        foreach (var chargeType in wholesaleServicesRequest.ChargeTypes)
        {
            chargeType.ChargeType_.Should().NotBeCimCode();
        }
    }

    [Fact]
    public async Task When_WholesaleServicesProcess_is_sent_service_bus_message_is_not_resent_to_wholesale()
    {
        // Arrange
        var marketMessage = InitializeProcessDtoBuilder()
            .Build();
        await InvokeCommandAsync(new InitializeWholesaleServicesProcessesCommand(marketMessage));
        await ProcessInternalCommandsAsync();
        _senderSpy.Reset();
        var process = GetProcess(marketMessage.Series.Single().RequestedByActor.ActorNumber);

        // Act
        process!.SendToWholesale();

        // Assert
        _senderSpy.LatestMessage.Should().BeNull();
        await AssertProcessState(marketMessage.MessageId, WholesaleServicesProcess.State.Sent);
    }

    [Fact]
    public async Task When_WholesaleServicesProcess_fails_to_send_service_bus_message_to_wholesale_state_is_initialized()
    {
        // Arrange
        var marketMessage = InitializeProcessDtoBuilder()
            .Build();
        _senderSpy.ShouldFail = true;

        // Act
        await InvokeCommandAsync(new InitializeWholesaleServicesProcessesCommand(marketMessage));
        await ProcessInternalCommandsAsync();

        // Assert
        _senderSpy.LatestMessage.Should().BeNull();
        await AssertProcessState(marketMessage.MessageId, WholesaleServicesProcess.State.Initialized);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _processContext.Dispose();
    }

    private static InitializeWholesaleServicesProcessDtoBuilder InitializeProcessDtoBuilder()
    {
        return new InitializeWholesaleServicesProcessDtoBuilder();
    }

    private async Task AssertProcessState(string messageId, WholesaleServicesProcess.State state)
    {
        var databaseConnectionFactory = GetService<IDatabaseConnectionFactory>();
        var sqlStatement =
            $"SELECT [State] FROM [dbo].[WholesaleServicesProcesses] WHERE [InitiatedByMessageId] = '{messageId}'";

        using var connection = await databaseConnectionFactory.GetConnectionAndOpenAsync(CancellationToken.None);
        var actualState = await connection.ExecuteScalarAsync<string>(sqlStatement);
        actualState.Should().Be(state.ToString());
    }

    private WholesaleServicesProcess? GetProcess(ActorNumber senderNumber)
    {
        return _processContext.WholesaleServicesProcesses
            .ToList()
            .FirstOrDefault(x => x.RequestedByActor.ActorNumber.Value == senderNumber.Value);
    }

    private sealed class ProcessAndRequestComparer : IEquivalencyStep
    {
        private readonly
            IReadOnlyDictionary<string, Action<WholesaleServicesProcess, InitializeWholesaleServicesProcessDto,
                InitializeWholesaleServicesSeries>>
            _assertionMap =
                new
                    Dictionary<string, Action<WholesaleServicesProcess, InitializeWholesaleServicesProcessDto,
                        InitializeWholesaleServicesSeries>>
                    {
                        {
                            nameof(WholesaleServicesProcess.ProcessId),
                            (p, r, s) => p.ProcessId.Id.Should().NotBeEmpty()
                        },
                        {
                            nameof(WholesaleServicesProcess.BusinessTransactionId),
                            (p, r, s) => p.BusinessTransactionId.Value.Should().Be(s.Id)
                        },
                        {
                            nameof(WholesaleServicesProcess.RequestedByActor),
                            (p, r, s) => p.RequestedByActor.Should().Be(s.RequestedByActor)
                        },
                        {
                            nameof(WholesaleServicesProcess.OriginalActor),
                            (p, r, s) => p.OriginalActor.Should().Be(s.OriginalActor)
                        },
                        {
                            nameof(WholesaleServicesProcess.BusinessReason),
                            (p, r, s) => p.BusinessReason.Code.Should().Be(r.BusinessReason)
                        },
                        {
                            nameof(WholesaleServicesProcess.StartOfPeriod),
                            (p, r, s) => p.StartOfPeriod.Should().Be(s.StartDateTime)
                        },
                        {
                            nameof(WholesaleServicesProcess.EndOfPeriod),
                            (p, r, s) => p.EndOfPeriod.Should().Be(s.EndDateTime)
                        },
                        {
                            nameof(WholesaleServicesProcess.RequestedGridArea),
                            (p, r, s) => p.RequestedGridArea.Should().Be(s.RequestedGridAreaCode)
                        },
                        {
                            nameof(WholesaleServicesProcess.GridAreas),
                            (p, r, s) => p.GridAreas.Should().Equal(s.GridAreas)
                        },
                        {
                            nameof(WholesaleServicesProcess.EnergySupplierId),
                            (p, r, s) => p.EnergySupplierId.Should().Be(s.EnergySupplierId)
                        },
                        {
                            nameof(WholesaleServicesProcess.Resolution),
                            (p, r, s) =>
                                p.Resolution.Should().Be(s.Resolution)
                        },
                        {
                            nameof(WholesaleServicesProcess.ChargeOwner),
                            (p, r, s) =>
                                p.ChargeOwner.Should().Be(s.ChargeOwner)
                        },
                        {
                            nameof(WholesaleServicesProcess.SettlementVersion), (p, r, s) =>
                            {
                                if (p.SettlementVersion is not null)
                                {
                                    p.SettlementVersion.Code.Should().Be(s.SettlementVersion);
                                }
                                else
                                {
                                    p.SettlementVersion.Should().BeNull();
                                }
                            }
                        },
                        {
                            nameof(WholesaleServicesProcess.ChargeTypes), (p, r, s) =>
                            {
                                p.ChargeTypes.Should().BeEquivalentTo(s.ChargeTypes);
                            }
                        },
                        {
                            nameof(WholesaleServicesProcess.InitiatedByMessageId),
                            (p, r, s) => p.InitiatedByMessageId.Value.Should().Be(r.MessageId)
                        },
                    };

        public EquivalencyResult Handle(
            Comparands comparands,
            IEquivalencyValidationContext context,
            IEquivalencyValidator nestedValidator)
        {
            if (comparands is not
                { Subject: WholesaleServicesProcess p, Expectation: InitializeWholesaleServicesProcessDto r })
            {
                return EquivalencyResult.ContinueWithNext;
            }

            var ignoredProperties = new[] { nameof(WholesaleServicesProcess.DomainEvents) };

            r.Series.Should().ContainSingle();

            using (new AssertionScope())
            {
                foreach (var propertyInfo in p.GetType()
                             .GetProperties()
                             .Where(propertyInfo => !ignoredProperties.Contains(propertyInfo.Name)))
                {
                    _assertionMap.Keys.Should().Contain(propertyInfo.Name);
                    _assertionMap[propertyInfo.Name](p, r, r.Series.First());
                }
            }

            return EquivalencyResult.AssertionCompleted;
        }
    }
}
