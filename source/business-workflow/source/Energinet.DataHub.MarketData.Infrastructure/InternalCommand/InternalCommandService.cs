using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketData.Application.ChangeOfSupplier;
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

            if (command.Type != null)
            {
                // var o = Activator.CreateInstance("Energinet.DataHub.MarketData.Application", command.Type);
                // var type = Type.GetType(command.Type);
                //
                //
                // var bent = o?.Unwrap();
                //
                //
                // if (bent == null) return;
                // var type = Type.GetType("Energinet.DataHub.MarketData.Application.ChangeOfSupplier." + command.Type) ?? throw new Exception();
                var res = _jsonSerializer.Deserialize<RequestChangeOfSupplier>(command.Data);
                // var res = Convert.ChangeType(command.Data, typeof(RequestChangeOfSupplier)) ?? throw new Exception();
                await _mediator.Send(res, CancellationToken.None);
            }
        }
    }
}
