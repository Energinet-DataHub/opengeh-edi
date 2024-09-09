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

using System.Reflection;
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
using Xunit.Abstractions;
using Xunit.Categories;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.IncomingMessages.RequestAggregatedMeasureData;

[IntegrationTest]
public class InitializeAggregatedMeasureDataProcessesCommandTests : TestBase
{
    private readonly ProcessContext _processContext;
    private readonly ServiceBusSenderSpy _senderSpy;
    private readonly ServiceBusSenderFactoryStub _serviceBusClientSenderFactory;

    public InitializeAggregatedMeasureDataProcessesCommandTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
        _processContext = GetService<ProcessContext>();
        _serviceBusClientSenderFactory = (ServiceBusSenderFactoryStub)GetService<IServiceBusSenderFactory>();
        _senderSpy = new ServiceBusSenderSpy("Fake");
        _serviceBusClientSenderFactory.AddSenderSpy(_senderSpy);
    }

    public static IEnumerable<object?[]> InitializeProcessData()
    {
        return new[]
        {
            // Return parameters: string? meteringPointType, string? requestedGridArea, string[] gridAreas

            // Metering point type has value, and the request is for all grid areas
            new object?[] { MeteringPointType.Consumption.Code, null, Array.Empty<string>() },

            // Metering point type is null and the request is for grid area 101
            new object?[] { null, "101", new[] { "101" } },

            // Metering point type is null and the request is for all grid areas,
            // but because of delegation the actual grid areas requested are 101 and 542
            new object?[] { null, null, new[] { "101", "542" } },
        };
    }

    [Theory]
    [MemberData(nameof(InitializeProcessData))]
    public async Task Aggregated_measure_data_process_is_created_and_has_correct_data(string? meteringPointType, string? requestedGridArea, string[] gridAreas)
    {
        ArgumentNullException.ThrowIfNull(gridAreas);

        // Arrange
        var initializeProcessDto = MessageBuilder()
            .SetMeteringPointType(meteringPointType)
            .SetRequestedGridArea(requestedGridArea)
            .SetGridAreas(gridAreas)
            .Build();

        // Act
        await InvokeCommandAsync(new InitializeAggregatedMeasureDataProcessesCommand(initializeProcessDto));

        // Assert
        var process = GetProcess(initializeProcessDto.SenderNumber);
        process.Should().NotBeNull();
        initializeProcessDto.Series.Should().NotBeEmpty();
        process!.BusinessTransactionId.Should().Be(initializeProcessDto.Series.First().Id);
        AssertProcessState(process, AggregatedMeasureDataProcess.State.Initialized);
        process.Should().BeEquivalentTo(initializeProcessDto, opt => opt.Using(new ProcessAndRequestComparer()));
    }

    [Fact]
    public async Task Aggregated_measure_data_process_was_sent_to_wholesale()
    {
        // Arrange
        var initializeProcessDto =
            MessageBuilder().
                SetSenderRole(ActorRole.MeteredDataResponsible.Code).
                SetEnergySupplierId(null).
                SetBalanceResponsibleId(null).
                Build();

        // Act
        await InvokeCommandAsync(new InitializeAggregatedMeasureDataProcessesCommand(initializeProcessDto));
        await ProcessInternalCommandsAsync();

        // Assert
        var exceptedServiceBusMessageSubject = nameof(AggregatedTimeSeriesRequest);
        var message = _senderSpy.LatestMessage;
        var process = GetProcess(initializeProcessDto.SenderNumber);
        Assert.NotNull(message);
        Assert.NotNull(process);
        Assert.Equal(process.ProcessId.Id.ToString(), message.MessageId);
        Assert.Equal(exceptedServiceBusMessageSubject, message.Subject);
        Assert.Equal(initializeProcessDto.Series.First().Id, process.BusinessTransactionId);
        AssertProcessState(process, AggregatedMeasureDataProcess.State.Sent);
    }

    [Fact]
    public async Task Aggregated_measure_data_process_without_settlement_method_was_sent_to_wholesale()
    {
        // Arrange
        var initializeProcessDto =
            MessageBuilder().
                SetSettlementMethod(null).
                Build();

        // Act
        await InvokeCommandAsync(new InitializeAggregatedMeasureDataProcessesCommand(initializeProcessDto));
        await ProcessInternalCommandsAsync();

        // Assert
        var message = _senderSpy.LatestMessage;
        Assert.NotNull(message);
    }

    [Fact]
    public async Task When_AggregatedTimeSeriesRequest_is_sent_to_wholesale_it_contains_no_CIM_codes()
    {
        // Arrange
        var initializeProcessDto = MessageBuilder()
            .Build();

        // Act
        await InvokeCommandAsync(new InitializeAggregatedMeasureDataProcessesCommand(initializeProcessDto));
        await ProcessInternalCommandsAsync();

        // Assert
        var message = _senderSpy.LatestMessage;

        using var scope = new AssertionScope();
        message.Should().NotBeNull();
        var aggregatedTimeSeriesRequest = AggregatedTimeSeriesRequest.Parser.ParseFrom(message!.Body);

        aggregatedTimeSeriesRequest.RequestedForActorRole.Should().NotBeCimCode();
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

    private static InitializeAggregatedMeasureDataProcessDtoBuilder MessageBuilder()
    {
        return new InitializeAggregatedMeasureDataProcessDtoBuilder();
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
            .FirstOrDefault(x => x.RequestedByActor.ActorNumber.Value == senderNumber);
    }

    private sealed class ProcessAndRequestComparer : IEquivalencyStep
    {
        private readonly
            IReadOnlyDictionary<string, Action<AggregatedMeasureDataProcess, InitializeAggregatedMeasureDataProcessDto, InitializeAggregatedMeasureDataProcessSeries>>
            _assertionMap =
                new Dictionary<string, Action<AggregatedMeasureDataProcess, InitializeAggregatedMeasureDataProcessDto, InitializeAggregatedMeasureDataProcessSeries>>
                {
                    {
                        nameof(AggregatedMeasureDataProcess.ProcessId),
                        (p, r, s) => p.ProcessId.Id.Should().NotBeEmpty()
                    },
                    {
                        nameof(AggregatedMeasureDataProcess.BusinessTransactionId),
                        (p, r, s) => p.BusinessTransactionId.Should().Be(s.Id)
                    },
                    {
                        nameof(AggregatedMeasureDataProcess.RequestedByActor),
                        (p, r, s) => p.RequestedByActor.Should().Be(s.RequestedByActor)
                    },
                    {
                        nameof(AggregatedMeasureDataProcess.OriginalActor),
                        (p, r, s) => p.OriginalActor.Should().Be(s.OriginalActor)
                    },
                    {
                        nameof(AggregatedMeasureDataProcess.BusinessReason),
                        (p, r, s) => p.BusinessReason.Code.Should().Be(r.BusinessReason)
                    },
                    {
                        nameof(AggregatedMeasureDataProcess.MeteringPointType),
                        (p, r, s) => p.MeteringPointType.Should().Be(s.MeteringPointType)
                    },
                    {
                        nameof(AggregatedMeasureDataProcess.SettlementMethod),
                        (p, r, s) => p.SettlementMethod.Should().Be(s.SettlementMethod)
                    },
                    {
                        nameof(AggregatedMeasureDataProcess.StartOfPeriod),
                        (p, r, s) => p.StartOfPeriod.Should().Be(s.StartDateTime)
                    },
                    {
                        nameof(AggregatedMeasureDataProcess.EndOfPeriod),
                        (p, r, s) => p.EndOfPeriod.Should().Be(s.EndDateTime)
                    },
                    {
                        nameof(AggregatedMeasureDataProcess.RequestedGridArea),
                        (p, r, s) => p.RequestedGridArea.Should().Be(s.RequestedGridAreaCode)
                    },
                    {
                        nameof(AggregatedMeasureDataProcess.GridAreas),
                        (p, r, s) => p.GridAreas.Should().BeEquivalentTo(s.GridAreas)
                    },
                    {
                        nameof(AggregatedMeasureDataProcess.EnergySupplierId),
                        (p, r, s) => p.EnergySupplierId.Should().Be(s.EnergySupplierNumber)
                    },
                    {
                        nameof(AggregatedMeasureDataProcess.BalanceResponsibleId),
                        (p, r, s) => p.BalanceResponsibleId.Should().Be(s.BalanceResponsibleNumber)
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
                { Subject: AggregatedMeasureDataProcess p, Expectation: InitializeAggregatedMeasureDataProcessDto r })
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
