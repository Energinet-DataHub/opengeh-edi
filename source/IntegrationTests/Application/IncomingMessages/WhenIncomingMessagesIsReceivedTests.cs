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
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.MessageBus;
using Energinet.DataHub.EDI.Common;
using Energinet.DataHub.EDI.Common.Actors;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.IntegrationTests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.IncomingMessages;

public class WhenIncomingMessagesIsReceivedTests : TestBase
{
    private readonly IIncomingMessageParser _incomingMessagesRequest;
    private readonly ServiceBusSenderFactoryStub _serviceBusClientSenderFactory;
    private readonly ServiceBusSenderSpy _senderSpy;

    public WhenIncomingMessagesIsReceivedTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _serviceBusClientSenderFactory = (ServiceBusSenderFactoryStub)GetService<IServiceBusSenderFactory>();
        _senderSpy = new ServiceBusSenderSpy("Fake");
        _serviceBusClientSenderFactory.AddSenderSpy(_senderSpy);
        _incomingMessagesRequest = GetService<IIncomingMessageParser>();
    }

    [Fact]
    public async Task Incoming_message_is_received()
    {
      // Assert
      var authenticatedActor = GetService<AuthenticatedActor>();
      authenticatedActor.SetAuthenticatedActor(new ActorIdentity(ActorNumber.Create("5799999933318"), Restriction.Owned, MarketRole.BalanceResponsibleParty));

      // Act
      await _incomingMessagesRequest.ParseAsync(
          ReadJsonFile("Application\\IncomingMessages\\RequestAggregatedMeasureData.json"),
          DocumentFormat.Json,
          DocumentType.NotifyAggregatedMeasureData,
          CancellationToken.None);

      // Assert
      var message = _senderSpy.Message;
      Assert.NotNull(message);
    }

    [Fact]
    public async Task Only_one_request_pr_transactionId_and_messageId_is_accepted()
    {
        // Arrange
        var authenticatedActor = GetService<AuthenticatedActor>();
        authenticatedActor.SetAuthenticatedActor(new ActorIdentity(ActorNumber.Create("5799999933318"), Restriction.Owned, MarketRole.BalanceResponsibleParty));

        // Act
        // new scope to simulate a race condition.
        var sessionProvider = GetService<IServiceProvider>();
        using var secondScope = sessionProvider.CreateScope();
        var authenticatedActorInSecondScope = secondScope.ServiceProvider.GetService<AuthenticatedActor>();
        var secondParser = secondScope.ServiceProvider.GetRequiredService<IIncomingMessageParser>();
        var task01 = _incomingMessagesRequest.ParseAsync(
            ReadJsonFile("Application\\IncomingMessages\\RequestAggregatedMeasureData.json"),
            DocumentFormat.Json,
            DocumentType.NotifyAggregatedMeasureData,
            CancellationToken.None);
        var task02 = secondParser.ParseAsync(
            ReadJsonFile("Application\\IncomingMessages\\RequestAggregatedMeasureData.json"),
            DocumentFormat.Json,
            DocumentType.NotifyAggregatedMeasureData,
            CancellationToken.None);
        authenticatedActorInSecondScope!.SetAuthenticatedActor(new ActorIdentity(ActorNumber.Create("5799999933318"), restriction: Restriction.None));

        try
        {
            await Task.WhenAll(task01, task02);
        }
        catch (DbUpdateException e)
        {
            // Assert
            // This exception is only expected if a command execution finishes before the other one ends.
            Assert.Contains("Violation of PRIMARY KEY constraint", e.InnerException?.Message, StringComparison.InvariantCulture);
        }

        // Assert
        var message = _senderSpy.Message;
        Assert.NotNull(message);
    }

    protected override void Dispose(bool disposing)
    {
        _senderSpy.Dispose();
        _serviceBusClientSenderFactory.Dispose();
        base.Dispose(disposing);
    }

    private static MemoryStream ReadJsonFile(string path)
    {
        var jsonDoc = File.ReadAllText(path);

        var stream = new MemoryStream();
        using var writer = new StreamWriter(stream: stream, encoding: Encoding.UTF8, bufferSize: 4096, leaveOpen: true);
        writer.Write(jsonDoc);
        writer.Flush();
        stream.Position = 0;

        return stream;
    }
}
