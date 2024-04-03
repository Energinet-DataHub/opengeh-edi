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
using System.Reflection;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.MessageBus;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.IntegrationTests.TestDoubles;
using Energinet.DataHub.EDI.Process.Application.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.Process.Interfaces;
using Energinet.DataHub.Edi.Requests;
using FluentAssertions;
using FluentAssertions.Equivalency;
using FluentAssertions.Execution;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.IncomingMessages.RequestAggregatedMeasureData;

[IntegrationTest]
public class InitializeAggregatedMeasureDataProcessesCommandTests : TestBase
{
    private readonly ProcessContext _processContext;
    private readonly ServiceBusSenderSpy _senderSpy;
    private readonly ServiceBusSenderFactoryStub _serviceBusClientSenderFactory;

    public InitializeAggregatedMeasureDataProcessesCommandTests(IntegrationTestFixture integrationTestFixture)
        : base(integrationTestFixture)
    {
        _processContext = GetService<ProcessContext>();
        _serviceBusClientSenderFactory = (ServiceBusSenderFactoryStub)GetService<IServiceBusSenderFactory>();
        _senderSpy = new ServiceBusSenderSpy("Fake");
        _serviceBusClientSenderFactory.AddSenderSpy(_senderSpy);
    }

    [Theory]
    [InlineData("E17")]
    [InlineData(null)]
    public async Task Aggregated_measure_data_process_is_created_and_has_correct_data(string? marketEvaluationPointType)
    {
        // Arrange
        var marketMessage = MessageBuilder()
            .SetMarketEvaluationPointType(marketEvaluationPointType)
            .Build();

        // Act
        await InvokeCommandAsync(new InitializeAggregatedMeasureDataProcessesCommand(marketMessage));

        // Assert
        var process = GetProcess(marketMessage.SenderNumber);
        process.Should().NotBeNull();
        marketMessage.Series.Should().NotBeEmpty();
        process!.BusinessTransactionId.Id.Should().Be(marketMessage.Series.First().Id);
        AssertProcessState(process, AggregatedMeasureDataProcess.State.Initialized);
        process.Should().BeEquivalentTo(marketMessage, opt => opt.Using(new ProcessAndRequestComparer()));
    }

    [Fact]
    public async Task Aggregated_measure_data_process_was_sent_to_wholesale()
    {
        // Arrange
        var marketMessage =
            MessageBuilder().
                SetSenderRole(ActorRole.MeteredDataResponsible.Code).
                SetEnergySupplierId(null).
                SetBalanceResponsibleId(null).
                Build();

        // Act
        await InvokeCommandAsync(new InitializeAggregatedMeasureDataProcessesCommand(marketMessage));
        await ProcessInternalCommandsAsync();

        // Assert
        var exceptedServiceBusMessageSubject = nameof(AggregatedTimeSeriesRequest);
        var message = _senderSpy.Message;
        var process = GetProcess(marketMessage.SenderNumber);
        Assert.NotNull(message);
        Assert.NotNull(process);
        Assert.Equal(process.ProcessId.Id.ToString(), message!.MessageId);
        Assert.Equal(exceptedServiceBusMessageSubject, message!.Subject);
        Assert.Equal(marketMessage.Series.First().Id, process!.BusinessTransactionId.Id);
        AssertProcessState(process, AggregatedMeasureDataProcess.State.Sent);
    }

    [Fact]
    public async Task Aggregated_measure_data_process_without_settlement_method_was_sent_to_wholesale()
    {
        // Arrange
        var marketMessage =
            MessageBuilder().
                SetMarketEvaluationSettlementMethod(null).
                Build();

        // Act
        await InvokeCommandAsync(new InitializeAggregatedMeasureDataProcessesCommand(marketMessage));
        await ProcessInternalCommandsAsync();

        // Assert
        var message = _senderSpy.Message;
        Assert.NotNull(message);
    }

    [Fact]
    public async Task When_AggregatedTimeSeriesRequest_is_sent_to_wholesale_it_contains_no_CIM_codes()
    {
        // Arrange
        var marketMessage = MessageBuilder()
            .Build();

        // Act
        await InvokeCommandAsync(new InitializeAggregatedMeasureDataProcessesCommand(marketMessage));
        await ProcessInternalCommandsAsync();

        // Assert
        var message = _senderSpy.Message;

        using var scope = new AssertionScope();
        message.Should().NotBeNull();
        var aggregatedTimeSeriesRequest = AggregatedTimeSeriesRequest.Parser.ParseFrom(message!.Body);

        aggregatedTimeSeriesRequest.RequestedByActorRole.Should().NotBeCimCode();
        aggregatedTimeSeriesRequest.BusinessReason.Should().NotBeCimCode();
        aggregatedTimeSeriesRequest.SettlementVersion.Should().NotBeCimCode();
        aggregatedTimeSeriesRequest.SettlementMethod.Should().NotBeCimCode();
        aggregatedTimeSeriesRequest.MeteringPointType.Should().NotBeCimCode();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _processContext.Dispose();
        _senderSpy.Dispose();
        _serviceBusClientSenderFactory.Dispose();
    }

    private static RequestAggregatedMeasureDataMarketDocumentBuilder MessageBuilder()
    {
        return new RequestAggregatedMeasureDataMarketDocumentBuilder();
    }

    private static void AssertProcessState(AggregatedMeasureDataProcess process, AggregatedMeasureDataProcess.State state)
    {
        var processState = typeof(AggregatedMeasureDataProcess).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(process);
        Assert.Equal(state, processState);
    }

    private AggregatedMeasureDataProcess? GetProcess(string senderNumber)
    {
        ClearDbContextCaches();

        return _processContext.AggregatedMeasureDataProcesses
            .ToList()
            .FirstOrDefault(x => x.RequestedByActorId.Value == senderNumber);
    }

    private sealed class ProcessAndRequestComparer : IEquivalencyStep
    {
        private readonly
            IReadOnlyDictionary<string, Action<AggregatedMeasureDataProcess, RequestAggregatedMeasureDataDto, Serie>>
            _assertionMap =
                new Dictionary<string, Action<AggregatedMeasureDataProcess, RequestAggregatedMeasureDataDto, Serie>>
                {
                    {
                        nameof(AggregatedMeasureDataProcess.ProcessId),
                        (p, r, s) => p.ProcessId.Id.Should().NotBeEmpty()
                    },
                    {
                        nameof(AggregatedMeasureDataProcess.BusinessTransactionId),
                        (p, r, s) => p.BusinessTransactionId.Id.Should().Be(s.Id)
                    },
                    {
                        nameof(AggregatedMeasureDataProcess.RequestedByActorId),
                        (p, r, s) => p.RequestedByActorId.Value.Should().Be(r.SenderNumber)
                    },
                    {
                        nameof(AggregatedMeasureDataProcess.RequestedByActorRoleCode),
                        (p, r, s) => p.RequestedByActorRoleCode.Should().Be(r.SenderRoleCode)
                    },
                    {
                        nameof(AggregatedMeasureDataProcess.BusinessReason),
                        (p, r, s) => p.BusinessReason.Code.Should().Be(r.BusinessReason)
                    },
                    {
                        nameof(AggregatedMeasureDataProcess.MeteringPointType),
                        (p, r, s) => p.MeteringPointType.Should().Be(s.MarketEvaluationPointType)
                    },
                    {
                        nameof(AggregatedMeasureDataProcess.SettlementMethod),
                        (p, r, s) => p.SettlementMethod.Should().Be(s.MarketEvaluationSettlementMethod)
                    },
                    {
                        nameof(AggregatedMeasureDataProcess.StartOfPeriod),
                        (p, r, s) => p.StartOfPeriod.Should().Be(s.StartDateAndOrTimeDateTime)
                    },
                    {
                        nameof(AggregatedMeasureDataProcess.EndOfPeriod),
                        (p, r, s) => p.EndOfPeriod.Should().Be(s.EndDateAndOrTimeDateTime)
                    },
                    {
                        nameof(AggregatedMeasureDataProcess.MeteringGridAreaDomainId),
                        (p, r, s) => p.MeteringGridAreaDomainId.Should().Be(s.MeteringGridAreaDomainId)
                    },
                    {
                        nameof(AggregatedMeasureDataProcess.EnergySupplierId),
                        (p, r, s) => p.EnergySupplierId.Should().Be(s.EnergySupplierMarketParticipantId)
                    },
                    {
                        nameof(AggregatedMeasureDataProcess.BalanceResponsibleId),
                        (p, r, s) => p.BalanceResponsibleId.Should().Be(s.BalanceResponsiblePartyMarketParticipantId)
                    },
                    {
                        nameof(AggregatedMeasureDataProcess.SettlementVersion), (p, r, s) =>
                        {
                            if (p.SettlementVersion is not null)
                            {
                                p.SettlementVersion.Code.Should().Be(s.SettlementVersion);
                            }
                            else
                            {
                                p.SettlementVersion.Should().BeNull();
                                p.SettlementVersion.Should().Be(s.SettlementVersion);
                            }
                        }
                    },
                    {
                        nameof(AggregatedMeasureDataProcess.InitiatedByMessageId),
                        (p, r, s) => p.InitiatedByMessageId.Value.Should().Be(r.MessageId)
                    },
                };

        public EquivalencyResult Handle(
            Comparands comparands,
            IEquivalencyValidationContext context,
            IEquivalencyValidator nestedValidator)
        {
            if (comparands is not
                { Subject: AggregatedMeasureDataProcess p, Expectation: RequestAggregatedMeasureDataDto r })
            {
                return EquivalencyResult.ContinueWithNext;
            }

            var ignoredProperties = new[] { nameof(AggregatedMeasureDataProcess.DomainEvents) };

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
