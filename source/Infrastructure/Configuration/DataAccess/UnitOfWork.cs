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
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.EDI.Infrastructure.Configuration.DataAccess;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly DbContext[] _contexts;

    public UnitOfWork(IEnumerable<UnitOfWorkDbContext> contexts)
    {
        ArgumentNullException.ThrowIfNull(contexts);

        _contexts = contexts.ToArray<DbContext>();

        if (_contexts.Length < 1)
        {
            throw new ArgumentException("At least one context must be provided", nameof(contexts));
        }
    }

    public async Task CommitTransactionAsync()
    {
        await ResilientTransaction
            .New(_contexts.Single(c => c.GetType() == typeof(ProcessContext)))
            .SaveChangesAsync(_contexts).ConfigureAwait(false);
    }
}
