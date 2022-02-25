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
using Energinet.DataHub.MarketRoles.Infrastructure.LocalMessageHub;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketRoles.Infrastructure.DataAccess.MessageHub
{
    public class MessageHubMessageRepository : IMessageHubMessageRepository
    {
        private readonly MarketRolesContext _context;

        public MessageHubMessageRepository(MarketRolesContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public Task<MessageHubMessage> GetMessageAsync(Guid messageId)
        {
            return _context.MessageHubMessages.SingleAsync(p => p.Id == messageId);
        }

        public Task<MessageHubMessage[]> GetMessagesAsync(Guid[] messageIds)
        {
            return _context.MessageHubMessages.Where(p => messageIds.Contains(p.Id)).ToArrayAsync();
        }

        public void AddMessageMetadata(MessageHubMessage messageHubMessage)
        {
            _context.MessageHubMessages.Add(messageHubMessage);
        }
    }
}
