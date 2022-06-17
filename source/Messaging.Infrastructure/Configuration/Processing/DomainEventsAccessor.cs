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

using System.Collections.Generic;
using System.Linq;
using Messaging.Domain.SeedWork;
using Messaging.Infrastructure.Configuration.DataAccess;

namespace Messaging.Infrastructure.Configuration.Processing;

public class DomainEventsAccessor
{
    private readonly B2BContext _context;

    public DomainEventsAccessor(B2BContext context)
    {
        _context = context;
    }

    public IReadOnlyCollection<DomainEvent> GetAllDomainEvents()
    {
        var domainEvents = _context.ChangeTracker
            .Entries<Entity>()
            .SelectMany(x => x.Entity.DomainEvents)
            .ToList();

        return domainEvents;
    }

    public void ClearAllDomainEvents()
    {
        _context.ChangeTracker
            .Entries<Entity>()
            .ToList()
            .ForEach(e => e.Entity.ClearDomainEvents());
    }
}
