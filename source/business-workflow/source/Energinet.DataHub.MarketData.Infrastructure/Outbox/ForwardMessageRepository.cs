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
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketData.Domain.SeedWork;
using Energinet.DataHub.MarketData.Infrastructure.DatabaseAccess.Write;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketData.Infrastructure.Outbox
{
    public class ForwardMessageRepository : IForwardMessageRepository
    {
        private readonly IWriteDatabaseContext _writeDatabaseContext;
        private readonly ISystemDateTimeProvider _systemDateTimeProvider;

        public ForwardMessageRepository(IWriteDatabaseContext writeDatabaseContext, ISystemDateTimeProvider systemDateTimeProvider)
        {
            _writeDatabaseContext = writeDatabaseContext;
            _systemDateTimeProvider = systemDateTimeProvider;
        }

        public async Task<ForwardMessage?> GetUnprocessedForwardMessageAsync()
        {
            return await _writeDatabaseContext.OutgoingActorMessageDataModels
                .Where(x => x.State == OutboxState.Pending.Id)
                .Select(x => new ForwardMessage
                {
                    Id = x.Id,
                    Data = x.Data,
                    Recipient = x.Recipient,
                    Type = x.Type,
                    OccurredOn = x.OccurredOn,
                }).FirstOrDefaultAsync();
        }

        public async Task MarkForwardedMessageAsProcessedAsync(Guid id)
        {
            var outgoingActorMessage = await _writeDatabaseContext.OutgoingActorMessageDataModels
                .SingleAsync(x => x.Id == id);

            outgoingActorMessage.LastUpdatedOn = _systemDateTimeProvider.Now();
        }
    }
}
