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

namespace Energinet.DataHub.MarketData.Domain.SeedWork
{
    /// <summary>
    /// Method for sending domain events
    /// </summary>
    /// <param name="domainEvent"><see cref="IDomainEvent"/> to publish</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> to cancel the operation</param>
    public delegate Task PublishDomainEvent(IDomainEvent domainEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Domain event context carries domain events that should be published when a request is done
    /// </summary>
    public class DomainEventsContext
    {
        private readonly List<IDomainEvent> _pendingDomainEvents;

        public DomainEventsContext()
            : this(new List<IDomainEvent>()) { }

        internal DomainEventsContext(List<IDomainEvent> domainEvents)
        {
            _pendingDomainEvents = domainEvents;
        }

        internal IReadOnlyCollection<IDomainEvent> DomainEvents => _pendingDomainEvents;

        /// <summary>
        /// Add a domain event
        /// </summary>
        /// <param name="domainEvent">event to store</param>
        /// <exception cref="ArgumentNullException"><paramref name="domainEvent"/> is null</exception>
        public void RecordDomainEvent(IDomainEvent domainEvent)
        {
            if (domainEvent == null) throw new ArgumentNullException(nameof(domainEvent));
            _pendingDomainEvents.Add(domainEvent);
        }

        /// <summary>
        /// Add a collection of <see cref="IDomainEvent"/>
        /// </summary>
        /// <param name="domainEvents">Events to add</param>
        /// <exception cref="ArgumentNullException"><paramref name="domainEvents"/> is null</exception>
        public void RecordDomainEvents(ICollection<IDomainEvent> domainEvents)
        {
            if (domainEvents == null) throw new ArgumentNullException(nameof(domainEvents));
            _pendingDomainEvents.AddRange(domainEvents);
        }

        /// <summary>
        /// Publish recorded events
        /// </summary>
        /// <param name="publishAction">How should the an event be published</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> for the operation</param>
        /// <exception cref="ArgumentNullException"><paramref name="publishAction"/> is null</exception>
        public async Task PublishEventsAsync(PublishDomainEvent publishAction, CancellationToken cancellationToken = default)
        {
            if (publishAction == null) throw new ArgumentNullException(nameof(publishAction));

            var num = 0;
            while (num < _pendingDomainEvents.Count)
            {
                await publishAction(_pendingDomainEvents[num], cancellationToken).ConfigureAwait(false);
                num++;
            }
        }
    }
}
