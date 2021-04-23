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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketData.Application.ChangeOfSupplier.Process.Commands;
using Energinet.DataHub.MarketData.Infrastructure.InternalCommand;
using GreenEnergyHub.Json;
using MediatR;
using Moq;
using Xunit;

namespace Energinet.DataHub.MarketData.Tests.InternalCommand
{
    [Trait("Category", "Unit")]
    public class InternalCommandServiceTests
    {
        private readonly Mock<IInternalCommandRepository> _internalCommandRepository;
        private readonly Mock<IMediator> _mediator;
        private readonly Mock<IJsonSerializer> _jsonSerializer;

        public InternalCommandServiceTests()
        {
            _internalCommandRepository = new Mock<IInternalCommandRepository>();
            _mediator = new Mock<IMediator>();
            _jsonSerializer = new Mock<IJsonSerializer>();

            _mediator.Setup(m => m.Send(It.IsAny<object>(), CancellationToken.None))
                .ReturnsAsync(new object());

            var fullyQualifiedPath = nameof(ChangeSupplier);

            _internalCommandRepository.SetupSequence(m => m.GetUnprocessedInternalCommandAsync())
                .ReturnsAsync(new Infrastructure.InternalCommand.InternalCommand
                {
                    Data = "Testvalue", Id = Guid.NewGuid(), Type = fullyQualifiedPath,
                })
                .ReturnsAsync((Infrastructure.InternalCommand.InternalCommand?)null);
        }

        [Fact]
        public async Task ExecuteUnprocessedInternalCommandsAsyncTest()
        {
            var sut = new InternalCommandService(
                _internalCommandRepository.Object,
                _mediator.Object,
                _jsonSerializer.Object);

            await sut.ExecuteUnprocessedInternalCommandsAsync();

            _mediator.Verify(m => m.Send(It.IsAny<object>(), CancellationToken.None), Times.Exactly(1));

            _internalCommandRepository.Verify(m => m.GetUnprocessedInternalCommandAsync(), Times.Exactly(2));
        }
    }
}
