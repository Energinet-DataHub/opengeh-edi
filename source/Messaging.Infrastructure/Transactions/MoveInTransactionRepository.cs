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

using Messaging.Application.Transactions;
using Messaging.Infrastructure.Configuration.DataAccess;

namespace Messaging.Infrastructure.Transactions
{
    public class MoveInTransactionRepository : IMoveInTransactionRepository
    {
        private readonly B2BContext _b2BContext;

        public MoveInTransactionRepository(B2BContext b2BContext)
        {
            _b2BContext = b2BContext;
        }

        public void Add(MoveInTransaction moveInTransaction)
        {
            _b2BContext.Transactions.Add(moveInTransaction);
        }

        public MoveInTransaction? GetById(string transactionId)
        {
            return _b2BContext.Transactions.Find(transactionId);
        }
    }
}
