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

using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Energinet.DataHub.MarketData.Application.ChangeOfSupplier.ActorMessages;
using Energinet.DataHub.MarketData.Infrastructure.Outbox;
using Energinet.DataHub.PostOffice.Contracts;
using Google.Protobuf;
using GreenEnergyHub.TestHelpers;
using Moq;
using NodaTime;
using NodaTime.Serialization.Protobuf;
using Xunit;

namespace Energinet.DataHub.MarketData.Tests.Outbox
{
    [Trait("Category", "Unit")]
    public class PostOfficeServiceTests
    {
        private Mock<ServiceBusSender> _serviceBusSender;

        public PostOfficeServiceTests()
        {
            _serviceBusSender = new Mock<ServiceBusSender>();

            _serviceBusSender.Setup(m => m.SendMessageAsync(It.IsAny<ServiceBusMessage>(), default))
                .Returns(Task.FromResult(typeof(void)));
        }

        [Fact]
        public async Task SendMessageAsyncTest()
        {
            var sut = new PostOfficeService(_serviceBusSender.Object);

            var document = new Document()
            {
                Content = "This is some data",
                Recipient = "1234",
                Type = typeof(RequestChangeOfSupplierRejected).ToString(),
                Version = "v1",
                CreationDate = SystemClock.Instance.GetCurrentInstant().ToTimestamp(),
            }.ToByteArray();

            await sut.SendMessageAsync(document).ConfigureAwait(false);

            _serviceBusSender.Verify(m => m.SendMessageAsync(It.IsAny<ServiceBusMessage>(), default), Times.Once);
        }
    }
}
