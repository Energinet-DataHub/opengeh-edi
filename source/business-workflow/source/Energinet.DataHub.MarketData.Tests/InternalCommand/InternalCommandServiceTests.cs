using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketData.Application.ChangeOfSupplier;
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
                    Data = "Testvalue", Id = 666, Type = fullyQualifiedPath,
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
