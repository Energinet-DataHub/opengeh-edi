using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketData.Infrastructure.Outbox;
using MediatR;
using Microsoft.Azure.WebJobs;

namespace Energinet.DataHub.MarketData.Infrastructure.InternalCommand
{
    public class InternalCommandService : IInternalCommandService
    {
        private readonly IInternalCommandRepository _internalCommandRepository;
        private readonly IMediator _mediator;

        public InternalCommandService(IInternalCommandRepository internalCommandRepository, IMediator mediator)
        {
            _internalCommandRepository = internalCommandRepository;
            _mediator = mediator;
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
                var type = Type.GetType(command.Type) ?? throw new Exception();

                var res = Convert.ChangeType(command.Data, type) ?? throw new Exception();

                await _mediator.Send(res, CancellationToken.None);
            }
        }
    }
}
