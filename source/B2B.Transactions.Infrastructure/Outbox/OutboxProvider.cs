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

using B2B.Transactions.Infrastructure.DataAccess;

namespace B2B.Transactions.Infrastructure.Outbox
{
    public class OutboxProvider : IOutbox
    {
        private readonly B2BContext _context;
        private readonly OutboxMessageFactory _outboxMessageFactory;

        public OutboxProvider(B2BContext context, OutboxMessageFactory outboxMessageFactory)
        {
            _context = context;
            _outboxMessageFactory = outboxMessageFactory;
        }

        public void Add<T>(T message)
        {
            var outboxMessage = _outboxMessageFactory.CreateFrom(message);
            _context.OutboxMessages.Add(outboxMessage);
        }
    }
}
