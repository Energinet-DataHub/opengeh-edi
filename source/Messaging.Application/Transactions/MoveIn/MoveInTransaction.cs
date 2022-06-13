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
using NodaTime;

namespace Messaging.Application.Transactions.MoveIn
{
    public class MoveInTransaction
    {
        private readonly List<object> _domainEvents = new List<object>();
        private bool _started;

        public MoveInTransaction(string transactionId, string marketEvaluationPointId, Instant effectiveDate, string? currentEnergySupplierId)
        {
            TransactionId = transactionId;
            MarketEvaluationPointId = marketEvaluationPointId;
            EffectiveDate = effectiveDate;
            CurrentEnergySupplierId = currentEnergySupplierId;
        }

        public string TransactionId { get; }

        public string? ProcessId { get; private set; }

        public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();

        public string MarketEvaluationPointId { get; }

        public Instant EffectiveDate { get; }

        public string? CurrentEnergySupplierId { get; }

        public void Start(BusinessRequestResult businessRequestResult)
        {
            if (businessRequestResult == null) throw new ArgumentNullException(nameof(businessRequestResult));
            if (businessRequestResult.Success == false)
            {
                _domainEvents.Add(new MoveInTransactionCompleted());
            }
            else
            {
                _started = true;
                _domainEvents.Add(new PendingBusinessProcess());
            }
        }

        public void Complete()
        {
            if (_started)
            {
                _domainEvents.Add(new MoveInTransactionCompleted());
            }
        }
    }
}
