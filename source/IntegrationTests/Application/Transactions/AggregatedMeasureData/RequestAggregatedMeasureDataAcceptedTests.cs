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
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Application.Configuration.DataAccess;
using Application.IncomingMessages.RequestAggregatedMeasureData;
using Application.Transactions.AggregatedMeasureData.Notifications;
using Dapper;
using Domain.Actors;
using Domain.OutgoingMessages.NotifyAggregatedMeasureData;
using Domain.Transactions;
using Domain.Transactions.AggregatedMeasureData;
using Google.Protobuf;
using Infrastructure.Configuration.MessageBus;
using Infrastructure.Transactions.AggregatedMeasureData;
using Infrastructure.WholeSale;
using IntegrationTests.Application.IncomingMessages;
using IntegrationTests.Fixtures;
using IntegrationTests.TestDoubles;
using NodaTime;
using Xunit;
using Xunit.Categories;

namespace IntegrationTests.Application.Transactions.AggregatedMeasureData;

[IntegrationTest]
public class RequestAggregatedMeasureDataAcceptedTests : TestBase
{
    private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
    private readonly ServiceBusSenderSpy _senderSpy;
    private readonly ServiceBusSenderFactoryStub _serviceBusClientSenderFactory;

    public RequestAggregatedMeasureDataAcceptedTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _databaseConnectionFactory = GetService<IDatabaseConnectionFactory>();
        _serviceBusClientSenderFactory = (ServiceBusSenderFactoryStub)GetService<IServiceBusSenderFactory>();
        _senderSpy = new ServiceBusSenderSpy("Fake");
        _serviceBusClientSenderFactory.AddSenderSpy(_senderSpy);
    }

    [Fact]
    public async Task Must_Throw_Exception_When_Process_Does_Not_Exist()
    {
        // Arrange
        var process = AggregatedMeasureDataProcessBuilder.Build(ProcessId.Create(Guid.NewGuid()));
        var wholesaleResponse = AggregatedMeasureDataProcessFactory.CreateResponseFromWholeSaleTemp(process);
        var inboxEvent = new InboxEvent(process.ProcessId.Id, nameof(wholesaleResponse), 2, wholesaleResponse);
        var serviceBusMessage = AggregatedMeasureDataProcessFactory.CreateServiceBusMessage(inboxEvent);
        await _senderSpy.SendAsync(serviceBusMessage, CancellationToken.None).ConfigureAwait(false);
        var message = _senderSpy.Message;
        // var aggregatedTimeSeries = AggregatedTimeSeriesRequestAccepted.Parser.ParseFrom(message!.Body);

        // Act
        //var command = new AggregatedMeasureDataAccepted(aggregatedTimeSeries, Guid.Parse(message.MessageId));
        var command = new AggregatedMeasureDataAccepted(message!.Body!.ToString(), Guid.Parse(message.MessageId));

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => InvokeCommandAsync(command)).ConfigureAwait(false);
    }

    // TODO AJW: When state is added to the process, this test should be updated to reflect that
    [Fact]
    public async Task Must_Throw_Exception_When_Process_State_Is_Initialized()
    {
        // Arrange
        var transactionCommand = MessageBuilder().Build();
        await InvokeCommandAsync(transactionCommand).ConfigureAwait(false);
        var process = await GetProcess(transactionCommand.MessageHeader.SenderId).ConfigureAwait(false);
        var wholesaleResponse = AggregatedMeasureDataProcessFactory.CreateResponseFromWholeSaleTemp(process!);
        var inboxEvent = new InboxEvent(process!.ProcessId.Id, nameof(wholesaleResponse), 2, wholesaleResponse);
        var serviceBusMessage = AggregatedMeasureDataProcessFactory.CreateServiceBusMessage(inboxEvent);
        await _senderSpy.SendAsync(serviceBusMessage, CancellationToken.None).ConfigureAwait(false);
        var message = _senderSpy.Message;

        // Act
        //var command = new AggregatedMeasureDataAccepted(aggregatedTimeSeries, Guid.Parse(message.MessageId));
        var command = new AggregatedMeasureDataAccepted(message!.Body!.ToString(), Guid.Parse(message.MessageId));

        // Assert
        await Assert.ThrowsAsync<AggregatedMeasureDataException>(() => InvokeCommandAsync(command))
            .ConfigureAwait(false);
    }

    [Fact]
    public async Task Internal_command_is_saved_as_outgoing_message()
    {
        var transactionCommand = MessageBuilder().Build();
        await InvokeCommandAsync(transactionCommand).ConfigureAwait(false);
        var process = await GetProcess(transactionCommand.MessageHeader.SenderId).ConfigureAwait(false);
        var wholesaleResponse = AggregatedMeasureDataProcessFactory.CreateResponseFromWholeSaleTemp(process!);
        var responseAsByteArray = new BinaryData(wholesaleResponse.ToByteArray());
        var wholesaleResponseAsString = Encoding.BigEndianUnicode.GetString(responseAsByteArray);
        var aggregatedMeasure = new AggregatedMeasureDataAccepted(wholesaleResponseAsString, process!.ProcessId.Id);

        var internalCommand = new AggregatedMeasureDataAcceptedInternalCommand(aggregatedMeasure);
        //await InvokeCommandAsync(internalCommand).ConfigureAwait(false);
        //var outgoingMessage = GetOutgoingMessage("1234567891234567");
        //Assert.NotNull(outgoingMessage);

        // When the state is properly implemented, this need to be deleted.
        await Assert.ThrowsAsync<AggregatedMeasureDataException>(() => InvokeCommandAsync(internalCommand))
            .ConfigureAwait(false);
    }

    private static RequestAggregatedMeasureDataMessageBuilder MessageBuilder()
    {
        return new RequestAggregatedMeasureDataMessageBuilder();
    }

    private async Task<AggregatedMeasureDataProcess?> GetProcess(string senderId)
    {
        using var connection = await _databaseConnectionFactory
            .GetConnectionAndOpenAsync(CancellationToken.None)
            .ConfigureAwait(false);
        var query = "SELECT * FROM dbo.AggregatedMeasureDataProcesses WHERE RequestedByActorId = @SenderId";
        var reader = await connection.ExecuteReaderAsync(query, new { SenderId = senderId }).ConfigureAwait(false);
        reader.Read();
        var processId = ProcessId.Create(Guid.Parse(reader["ProcessId"].ToString() ?? string.Empty));
        var businessTransactionId = BusinessTransactionId.Create(reader["BusinessTransactionId"].ToString()!);
        var requestedByActorId = ActorNumber.Create(reader["requestedByActorId"].ToString()!);
        var settlementVersion = reader["SettlementVersion"].ToString() ?? string.Empty;
        var meteringPointType = reader["MeteringPointType"].ToString() ?? string.Empty;
        var settlementMethod = reader["SettlementMethod"].ToString() ?? string.Empty;
        var startOfPeriod = Instant.FromDateTimeUtc(
            DateTime.Parse(
                reader["StartOfPeriod"].ToString()!,
                CultureInfo.CurrentCulture)
                .ToUniversalTime());
        var endOfPeriod = Instant.FromDateTimeUtc(
            DateTime.Parse(
                    reader["endOfPeriod"].ToString()!,
                    CultureInfo.CurrentCulture)
                .ToUniversalTime());
        var meteringGridAreaDomainId = reader["MeteringGridAreaDomainId"].ToString() ?? string.Empty;
        var energySupplierId = reader["EnergySupplierId"].ToString() ?? string.Empty;
        var balanceResponsibleId = reader["BalanceResponsibleId"].ToString() ?? string.Empty;

        var process = new AggregatedMeasureDataProcess(
            processId,
            businessTransactionId,
            requestedByActorId,
            settlementVersion,
            meteringPointType,
            settlementMethod,
            startOfPeriod,
            endOfPeriod,
            meteringGridAreaDomainId,
            energySupplierId,
            balanceResponsibleId);

        return process;
    }

    private async Task<AggregationResultMessage?> GetOutgoingMessage(string receiverId)
    {
        using var connection = await _databaseConnectionFactory
            .GetConnectionAndOpenAsync(CancellationToken.None)
            .ConfigureAwait(false);
        var query = "SELECT * FROM dbo.OutgoingMessages WHERE ReceiverId = @ReceiverId";
        var result = await connection
            .QueryAsync(query, new { ReceiverId = receiverId }).ConfigureAwait(false);

        ArgumentNullException.ThrowIfNull(result);
        return result?.ToList().FirstOrDefault();
    }

    private async Task DisposeAsync()
    {
        await _senderSpy.DisposeAsync().ConfigureAwait(false);
        await _serviceBusClientSenderFactory.DisposeAsync().ConfigureAwait(false);
    }
}
