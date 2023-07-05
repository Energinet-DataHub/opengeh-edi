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
using System.Threading;
using System.Threading.Tasks;
using Application.Configuration.DataAccess;
using Application.Transactions.AggregatedMeasureData.Notifications;
using Dapper;
using Domain.Transactions;
using Infrastructure.Configuration.MessageBus;
using IntegrationTests.Application.IncomingMessages;
using IntegrationTests.Fixtures;
using IntegrationTests.TestDoubles;
using Xunit;
using Xunit.Categories;

namespace IntegrationTests.Application.Transactions.AggregatedMeasureData;

[IntegrationTest]
public class RequestAggregatedMeasureDataFromWholesaleTests : TestBase
{
    private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
    private readonly ServiceBusSenderSpy _senderSpy;
    private readonly ServiceBusSenderFactoryStub _serviceBusClientSenderFactory;

    public RequestAggregatedMeasureDataFromWholesaleTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _databaseConnectionFactory = GetService<IDatabaseConnectionFactory>();
        _serviceBusClientSenderFactory = (ServiceBusSenderFactoryStub)GetService<IServiceBusSenderFactory>();
        _senderSpy = new ServiceBusSenderSpy("Fake");
        _serviceBusClientSenderFactory.AddSenderSpy(_senderSpy);
    }

    // TODO AJH START HERE 04-07-2023
    [Fact]
    public async Task Aggregated_measure_data_process_is_created()
    {
        var incomingMessage = MessageBuilder()
            .Build();

        await InvokeCommandAsync(incomingMessage).ConfigureAwait(false);
        var processId = await GetProcessId(incomingMessage.MessageHeader.SenderId).ConfigureAwait(false);
        var command = new NotifyWholesaleOfAggregatedMeasureDataRequest(processId);
        await InvokeCommandAsync(command).ConfigureAwait(false);
        var m = _senderSpy.Message;
        Assert.NotNull(processId);
    }

    private static RequestAggregatedMeasureDataMessageBuilder MessageBuilder()
    {
        return new RequestAggregatedMeasureDataMessageBuilder();
    }

    private async Task<ProcessId> GetProcessId(string senderId)
    {
        using var connection = await _databaseConnectionFactory.GetConnectionAndOpenAsync(CancellationToken.None).ConfigureAwait(false);
        var dictionary = (IDictionary<string, object>)await connection.QuerySingleAsync(
            $"SELECT * FROM dbo.AggregatedMeasureDataProcesses WHERE RequestedByActorId = @SenderId",
            new
            {
                @SenderId = senderId,
            }).ConfigureAwait(false);

        return ProcessId.Create(Guid.Parse(dictionary["ProcessId"].ToString() ?? string.Empty));
    }

    private async Task DisposeAsync()
    {
        await _senderSpy.DisposeAsync().ConfigureAwait(false);
        await _serviceBusClientSenderFactory.DisposeAsync().ConfigureAwait(false);
    }
}
