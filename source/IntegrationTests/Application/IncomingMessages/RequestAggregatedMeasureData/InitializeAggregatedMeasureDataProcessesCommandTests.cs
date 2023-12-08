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
using Energinet.DataHub.EDI.Common.Serialization;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.IntegrationTests.TestDoubles;
using Energinet.DataHub.EDI.Process.Application.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Process.Application.Transactions.AggregatedMeasureData.Commands;
using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.Process.Infrastructure.InternalCommands;
using Energinet.DataHub.EDI.Process.Interfaces;
using Energinet.DataHub.Edi.Requests;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.IncomingMessages.RequestAggregatedMeasureData;

[IntegrationTest]
public class InitializeAggregatedMeasureDataProcessesCommandTests : TestBase
{
    private readonly ProcessContext _processContext;
    private readonly ServiceBusSenderSpy _senderSpy;
    private readonly ServiceBusSenderFactoryStub _serviceBusClientSenderFactory;
    private readonly InternalCommandMapper _mapper;
    private readonly ISerializer _serializer;

    public InitializeAggregatedMeasureDataProcessesCommandTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _processContext = GetService<ProcessContext>();
        _mapper = GetService<InternalCommandMapper>();
        _serializer = GetService<ISerializer>();
        _serviceBusClientSenderFactory = (ServiceBusSenderFactoryStub)GetService<IServiceBusSenderFactory>();
        _senderSpy = new ServiceBusSenderSpy("Fake");
        _serviceBusClientSenderFactory.AddSenderSpy(_senderSpy);
    }

    [Fact]
    public async Task Aggregated_measure_data_process_is_created_and_has_correct_data()
    {
        // Arrange
        var marketMessage = MessageBuilder().Build();

        // Act
        await InvokeCommandAsync(new InitializeAggregatedMeasureDataProcessesCommand(marketMessage));

        // Assert
        var process = GetProcess(marketMessage.SenderNumber);
        Assert.Equal(marketMessage.Series.First().Id, process!.BusinessTransactionId.Id);
        AssertProcessState(process, AggregatedMeasureDataProcess.State.Initialized);
        AssertProcessValues(process, marketMessage);
    }

    [Fact]
    public async Task Aggregated_measure_data_process_was_sent_to_wholesale()
    {
        // Arrange
        var marketMessage =
            MessageBuilder().
                SetSenderRole(MarketRole.MeteredDataResponsible.Code).
                SetEnergySupplierId(null).
                SetBalanceResponsibleId(null).
                Build();
        await InvokeCommandAsync(new InitializeAggregatedMeasureDataProcessesCommand(marketMessage));
        var command = LoadCommand(nameof(SendAggregatedMeasureRequestToWholesale));
        var exceptedServiceBusMessageSubject = nameof(AggregatedTimeSeriesRequest);

        // Act
        await InvokeCommandAsync(command);

        // Assert
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
        await InvokeCommandAsync(new InitializeAggregatedMeasureDataProcessesCommand(marketMessage));
        var command = LoadCommand(nameof(SendAggregatedMeasureRequestToWholesale));

        // Act
        await InvokeCommandAsync(command);

        // Assert
        var message = _senderSpy.Message;
        Assert.NotNull(message);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _processContext.Dispose();
        _senderSpy.Dispose();
        _serviceBusClientSenderFactory.Dispose();
    }

    private static void AssertProcessValues(AggregatedMeasureDataProcess process, RequestAggregatedMeasureDataDto dto)
    {
        var marketMessageSerie = dto.Series.Single();

        Assert.NotEqual(Guid.Empty, process.ProcessId.Id);
        Assert.Equal(marketMessageSerie.Id, process.BusinessTransactionId.Id);
        Assert.Equal(dto.SenderNumber, process.RequestedByActorId.Value);
        Assert.Equal(dto.SenderRoleCode, process.RequestedByActorRoleCode);
        Assert.Equal(dto.BusinessReason, process.BusinessReason.Code);
        Assert.Equal(marketMessageSerie.MarketEvaluationPointType, process.MeteringPointType);
        Assert.Equal(marketMessageSerie.MarketEvaluationSettlementMethod, process.SettlementMethod);
        Assert.Equal(marketMessageSerie.StartDateAndOrTimeDateTime, process.StartOfPeriod);
        Assert.Equal(marketMessageSerie.EndDateAndOrTimeDateTime, process.EndOfPeriod);
        Assert.Equal(marketMessageSerie.MeteringGridAreaDomainId, process.MeteringGridAreaDomainId);
        Assert.Equal(marketMessageSerie.EnergySupplierMarketParticipantId, process.EnergySupplierId);
        Assert.Equal(marketMessageSerie.BalanceResponsiblePartyMarketParticipantId, process.BalanceResponsibleId);
        Assert.Equal(marketMessageSerie.SettlementSeriesVersion, process.SettlementVersion?.Code);

        // Assert makes sure we have tests for alle expected properties - this fails if we add another property to our process without testing it & adding it to the array below
        var assertedProperties = new[]
        {
            nameof(process.ProcessId),
            nameof(process.BusinessTransactionId),
            nameof(process.RequestedByActorId),
            nameof(process.RequestedByActorRoleCode),
            nameof(process.BusinessReason),
            nameof(process.MeteringPointType),
            nameof(process.SettlementMethod),
            nameof(process.StartOfPeriod),
            nameof(process.EndOfPeriod),
            nameof(process.MeteringGridAreaDomainId),
            nameof(process.EnergySupplierId),
            nameof(process.BalanceResponsibleId),
            nameof(process.SettlementVersion),
        };

        var ignoredProperties = new[]
        {
            nameof(AggregatedMeasureDataProcess.DomainEvents),
        };

        foreach (var propertyInfo in process.GetType().GetProperties())
        {
            if (!ignoredProperties.Contains(propertyInfo.Name))
                Assert.Contains(propertyInfo.Name, assertedProperties);
        }
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

    private InternalCommand LoadCommand(string internalCommandType)
    {
        var queuedInternalCommand = _processContext.QueuedInternalCommands
            .ToList()
            .First(x => x.Type == internalCommandType);

        var commandMetaData = _mapper.GetByName(queuedInternalCommand.Type);
        return (InternalCommand)_serializer.Deserialize(queuedInternalCommand.Data, commandMetaData.CommandType);
    }

    private AggregatedMeasureDataProcess? GetProcess(string senderNumber)
    {
        return _processContext.AggregatedMeasureDataProcesses
            .ToList()
            .FirstOrDefault(x => x.RequestedByActorId.Value == senderNumber);
    }

    private IEnumerable<AggregatedMeasureDataProcess> GetProcesses(string senderNumber)
    {
        return _processContext.AggregatedMeasureDataProcesses
            .ToList()
            .Where(x => x.RequestedByActorId.Value == senderNumber);
    }
}
