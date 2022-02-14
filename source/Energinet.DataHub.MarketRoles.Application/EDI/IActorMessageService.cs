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
using System.Threading.Tasks;
using NodaTime;

namespace Energinet.DataHub.MarketRoles.Application.EDI
{
    /// <summary>
    /// Create business documents.
    /// </summary>
    public interface IActorMessageService
    {
        /// <summary>
        /// Generic Notification (RSM004).
        /// </summary>
        Task SendGenericNotificationMessageAsync(
            string transactionId,
            string gsrn,
            Instant startDateAndOrTime,
            string receiverGln);

        /// <summary>
        /// Confirmation of change of supplier.
        /// </summary>
        Task SendChangeOfSupplierConfirmAsync(
            string transactionId,
            string gsrn);

        /// <summary>
        /// Rejection of change of supplier.
        /// </summary>
        Task SendChangeOfSupplierRejectAsync(
            string transactionId,
            string gsrn,
            IEnumerable<ErrorMessage> errors);
    }
}
