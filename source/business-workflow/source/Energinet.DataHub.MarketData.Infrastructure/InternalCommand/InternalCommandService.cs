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
