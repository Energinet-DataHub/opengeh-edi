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
using MediatR;
using Messaging.Infrastructure.Configuration.SystemTime;

namespace Messaging.Infrastructure.Configuration.InternalCommands
{
    public class ProcessInternalCommandsOnTimeHasPassed : INotificationHandler<TenSecondsHasHasPassed>
    {
        private readonly InternalCommandProcessor _internalCommandProcessor;

        public ProcessInternalCommandsOnTimeHasPassed(InternalCommandProcessor internalCommandProcessor)
        {
            _internalCommandProcessor = internalCommandProcessor ?? throw new ArgumentNullException(nameof(internalCommandProcessor));
        }

        public Task Handle(TenSecondsHasHasPassed notification, CancellationToken cancellationToken)
        {
            return _internalCommandProcessor.ProcessPendingAsync();
        }
    }
}
