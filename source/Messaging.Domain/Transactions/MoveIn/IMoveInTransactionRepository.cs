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

using NodaTime;

namespace Messaging.Domain.Transactions.MoveIn
{
    /// <summary>
    /// Storage for transactions
    /// </summary>
    public interface IMoveInTransactionRepository
    {
        /// <summary>
        /// Adds a transaction to store
        /// </summary>
        /// <param name="moveInTransaction"></param>
        void Add(MoveInTransaction moveInTransaction);

        /// <summary>
        /// Find a transaction by transaction id
        /// </summary>
        /// <param name="transactionId"></param>
        /// <returns><see cref="MoveInTransaction"/></returns>
        MoveInTransaction? GetById(string transactionId);

        /// <summary>
        /// Find transaction by business process id
        /// </summary>
        /// <param name="processId"></param>
        /// <returns><see cref="MoveInTransaction"/></returns>
        Task<MoveInTransaction?> GetByProcessIdAsync(string processId);

        /// <summary>
        /// Find transaction id by effective date and metering point number
        /// </summary>
        /// <param name="meteringPointNumber">GSRN number of the metering point where transaction is invoked</param>
        /// /// <param name="effectiveDate">Effective date of the move in transaction</param>
        /// <returns><see cref="MoveInTransaction"/></returns>
        Task<MoveInTransaction?> GetByEffectiveDateAsync(string meteringPointNumber, Instant effectiveDate);
    }
}
