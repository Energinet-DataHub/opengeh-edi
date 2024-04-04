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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.MessageBus;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.IntegrationTests.TestDoubles;
using Energinet.DataHub.EDI.Process.Application.Transactions.WholesaleServices;
using Energinet.DataHub.EDI.Process.Domain.Transactions.WholesaleServices;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.Process.Interfaces;
using Energinet.DataHub.Edi.Requests;
using FluentAssertions;
using FluentAssertions.Equivalency;
using FluentAssertions.Execution;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.IncomingMessages.WholesaleServices;

[IntegrationTest]
public class WhenWholesaleServicesIsRequestedTests : TestBase
{
    private readonly ProcessContext _processContext;
    private readonly ServiceBusSenderFactoryStub _serviceBusClientSenderFactory;
    private readonly ServiceBusSenderSpy _senderSpy;

    public WhenWholesaleServicesIsRequestedTests(IntegrationTestFixture integrationTestFixture)
        : base(integrationTestFixture)
    {
        _processContext = GetService<ProcessContext>();
        _serviceBusClientSenderFactory = (ServiceBusSenderFactoryStub)GetService<IServiceBusSenderFactory>();
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
        var process = GetProcess(marketMessage.SenderNumber);
        process.Should().NotBeNull();
        process!.BusinessTransactionId.Id.Should().Be(marketMessage.Serie.First().Id);
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
        var process = GetProcess(marketMessage.SenderNumber);
        var message = _senderSpy.Message;
        message.Should().NotBeNull();
        message!.Subject.Should().Be(exceptedServiceBusMessageSubject);
        process.Should().BeEquivalentTo(marketMessage, opt => opt.Using(new ProcessAndRequestComparer()));
        await AssertProcessState(marketMessage!.MessageId, WholesaleServicesProcess.State.Sent);
    }

    [Fact]
    public async Task When_WholesaleServicesProcess_is_initialized_with_unknown_business_reason_service_bus_message_is_sent_to_wholesale()
    {
        // Arrange
        var unknownBusinessReason = "A47";
        var marketMessage = InitializeProcessDtoBuilder()
            .SetBusinessReason(unknownBusinessReason)
            .Build();

        // Act
        await InvokeCommandAsync(new InitializeWholesaleServicesProcessesCommand(marketMessage));
        await ProcessInternalCommandsAsync();

        // Assert
        var process = GetProcess(marketMessage.SenderNumber);
        var message = _senderSpy.Message;
        message.Should().NotBeNull();
        process!.BusinessReason.Code.Should().Be(unknownBusinessReason);
        process.BusinessReason.Name.Should().Be("UNKNOWN");
        await AssertProcessState(marketMessage!.MessageId, WholesaleServicesProcess.State.Sent);
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
        var message = _senderSpy.Message;

        using var scope = new AssertionScope();
        message.Should().NotBeNull();
        var wholesaleServicesRequest = WholesaleServicesRequest.Parser.ParseFrom(message!.Body);

        wholesaleServicesRequest.RequestedByActorRole.Should().NotBeCimCode();
        wholesaleServicesRequest.BusinessReason.Should().NotBeCimCode();
        wholesaleServicesRequest.Resolution.Should().NotBeCimCode();
        wholesaleServicesRequest.SettlementSeriesVersion.Should().NotBeCimCode();
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
        _senderSpy.Message = null; // Reset the spy
        var process = GetProcess(marketMessage.SenderNumber);

        // Act
        process!.SendToWholesale();

        // Assert
        _senderSpy.Message.Should().BeNull();
        await AssertProcessState(marketMessage!.MessageId, WholesaleServicesProcess.State.Sent);
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
        _senderSpy.Message.Should().BeNull();
        await AssertProcessState(marketMessage!.MessageId, WholesaleServicesProcess.State.Initialized);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _processContext.Dispose();
        _senderSpy.Dispose();
        _serviceBusClientSenderFactory.Dispose();
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

    private WholesaleServicesProcess? GetProcess(string senderNumber)
    {
        return _processContext.WholesaleServicesProcesses
            .ToList()
            .FirstOrDefault(x => x.RequestedByActorId.Value == senderNumber);
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
                            (p, r, s) => p.BusinessTransactionId.Id.Should().Be(s.Id)
                        },
                        {
                            nameof(WholesaleServicesProcess.RequestedByActorId),
                            (p, r, s) => p.RequestedByActorId.Value.Should().Be(r.SenderNumber)
                        },
                        {
                            nameof(WholesaleServicesProcess.RequestedByActorRoleCode),
                            (p, r, s) => p.RequestedByActorRoleCode.Should().Be(r.SenderRoleCode)
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
                            nameof(WholesaleServicesProcess.GridAreaCode),
                            (p, r, s) => p.GridAreaCode.Should().Be(s.GridAreaCode)
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
                                    p.SettlementVersion.Code.Should().Be(s.SettlementSeriesVersion);
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

            r.Serie.Should().ContainSingle();

            using (new AssertionScope())
            {
                foreach (var propertyInfo in p.GetType()
                             .GetProperties()
                             .Where(propertyInfo => !ignoredProperties.Contains(propertyInfo.Name)))
                {
                    _assertionMap.Keys.Should().Contain(propertyInfo.Name);
                    _assertionMap[propertyInfo.Name](p, r, r.Serie.First());
                }
            }

            return EquivalencyResult.AssertionCompleted;
        }
    }
}
