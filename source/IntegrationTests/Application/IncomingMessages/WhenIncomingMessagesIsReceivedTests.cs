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

using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.MessageBus;
using Energinet.DataHub.EDI.Common;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.IntegrationTests.TestDoubles;
using Xunit;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.IncomingMessages;

public class WhenIncomingMessagesIsReceivedTests : TestBase
{
    private readonly IIncomingRequestAggregatedMeasuredParser _incomingMessagesRequest;
    private readonly ServiceBusSenderFactoryStub _serviceBusClientSenderFactory;
    private readonly ServiceBusSenderSpy _senderSpy;

    public WhenIncomingMessagesIsReceivedTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _serviceBusClientSenderFactory = (ServiceBusSenderFactoryStub)GetService<IServiceBusSenderFactory>();
        _senderSpy = new ServiceBusSenderSpy("Fake");
        _serviceBusClientSenderFactory.AddSenderSpy(_senderSpy);
        _incomingMessagesRequest = GetService<IIncomingRequestAggregatedMeasuredParser>();
    }

    [Fact]
    public async Task Incoming_message_is_received()
    {
      var respons = await _incomingMessagesRequest.ParseAsync(
            ReadJsonFile("Application\\IncomingMessages\\RequestAggregatedMeasureData.json"),
            DocumentFormat.Json,
            CancellationToken.None);

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
