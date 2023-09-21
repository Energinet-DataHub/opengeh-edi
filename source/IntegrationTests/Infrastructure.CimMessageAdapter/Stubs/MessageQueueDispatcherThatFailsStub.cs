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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.IncomingMessages;
using CimMessageAdapter.Messages;
using CimMessageAdapter.Messages.Queues;

namespace IntegrationTests.Infrastructure.CimMessageAdapter.Stubs
{
    public class MessageQueueDispatcherThatFailsStub<TQueue> : MessageQueueDispatcherStub<TQueue>, IMessageQueueDispatcher<TQueue>
    where TQueue : Queue
    {
        private readonly List<IMarketTransaction> _uncommittedItems = new();

        public new Task AddAsync(IMarketTransaction message, CancellationToken cancellationToken)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            _uncommittedItems.Add(message);
            return Task.CompletedTask;
        }

        public new Task CommitAsync(CancellationToken cancellationToken)
        {
            _uncommittedItems.Clear();
            throw new ServiceBusCommitException();
        }
    }

#pragma warning disable SA1402
    public class ServiceBusCommitException : Exception
#pragma warning restore SA1402
    {
        public ServiceBusCommitException(string message)
            : base(message)
        {
        }

        public ServiceBusCommitException()
        {
        }

        public ServiceBusCommitException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
