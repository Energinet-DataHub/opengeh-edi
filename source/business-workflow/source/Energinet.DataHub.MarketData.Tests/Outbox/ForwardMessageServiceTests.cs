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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Energinet.DataHub.MarketData.Infrastructure.Outbox;
using GreenEnergyHub.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;
using NodaTime;
using Xunit;
using Xunit.Sdk;

namespace Energinet.DataHub.MarketData.Tests.Outbox
{
    [Trait("Category", "Unit")]
    [Trait("Category", "Unit")]
    public class ForwardMessageServiceTests
    {
        private readonly Mock<IForwardMessageRepository> _forwardMessageRepositoryMock;
        private readonly Mock<IPostOfficeService> _postOfficeServiceMock;

        public ForwardMessageServiceTests()
        {
            _forwardMessageRepositoryMock = new Mock<IForwardMessageRepository>();
            _postOfficeServiceMock = new Mock<IPostOfficeService>();
            _forwardMessageRepositoryMock.SetupSequence(m => m.GetUnprocessedForwardMessageAsync())
                .ReturnsAsync(new ForwardMessage
                {
                    Id = 1,
                    Type = "something",
                    OccurredOn = default(Instant),
                    Data = "{'Name':'Boom'}",
                    Recipient = "1234567890",
                })
                .ReturnsAsync(new ForwardMessage
                {
                    Id = 2,
                    Type = "something twice",
                    OccurredOn = default(Instant),
                    Data = "{'Name':'Boomer'}",
                    Recipient = "1234567890",
                })
                .ReturnsAsync((ForwardMessage?)null);
            _forwardMessageRepositoryMock.Setup(m => m.MarkForwardedMessageAsProcessedAsync(It.IsAny<int>()))
                .Returns(Task.FromResult(typeof(void)));
        }

        [Fact]
        public async Task ProcessMessagesTest()
        {
            var sut = new ForwardMessageService(_forwardMessageRepositoryMock.Object, _postOfficeServiceMock.Object);

            await sut.ProcessMessagesAsync().ConfigureAwait(false);

            _forwardMessageRepositoryMock.Verify(m => m.GetUnprocessedForwardMessageAsync(), Times.Exactly(3));
            _forwardMessageRepositoryMock.Verify(m => m.MarkForwardedMessageAsProcessedAsync(It.IsAny<int>()), Times.Exactly(2));
            _postOfficeServiceMock.Verify(m => m.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(2));
        }
    }
}
