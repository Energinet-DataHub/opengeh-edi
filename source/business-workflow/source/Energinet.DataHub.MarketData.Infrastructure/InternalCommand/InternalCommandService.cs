using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GreenEnergyHub.Json;
using MediatR;

namespace Energinet.DataHub.MarketData.Infrastructure.InternalCommand
{
    public class InternalCommandService : IInternalCommandService
    {
        private readonly IInternalCommandRepository _internalCommandRepository;
        private readonly IMediator _mediator;
        private readonly IJsonSerializer _jsonSerializer;

        public InternalCommandService(IInternalCommandRepository internalCommandRepository, IMediator mediator, IJsonSerializer jsonSerializer)
        {
            _internalCommandRepository = internalCommandRepository;
            _mediator = mediator;
            _jsonSerializer = jsonSerializer;
        }

        public async Task ExecuteUnprocessedInternalCommandsAsync()
        {
            var command = await _internalCommandRepository.GetUnprocessedInternalCommandAsync().ConfigureAwait(false);

            if (command?.Type != null)
            {
                object parsedCommand = _jsonSerializer.Deserialize(command.Data, MessageTypeFactory.GetType(command.Type));

                await _mediator.Send(parsedCommand, CancellationToken.None);

                await ExecuteUnprocessedInternalCommandsAsync();
            }
        }
    }
}
