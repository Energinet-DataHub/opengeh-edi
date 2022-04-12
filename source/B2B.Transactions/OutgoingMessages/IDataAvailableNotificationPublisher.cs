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

using System.Threading.Tasks;
using Energinet.DataHub.MessageHub.Model.Model;

namespace B2B.Transactions.OutgoingMessages
{
    /// <summary>
    /// Interface for data available notifications
    /// </summary>
    public interface IDataAvailableNotificationPublisher
    {
        /// <summary>
        /// send the specified DataAvailableNotification to the post office DataAvailable queue.
        /// </summary>
        /// <param name="correlationId">The correlation id that can be used to track the data represented by the notification.</param>
        /// <param name="dataAvailableNotificationDto">The notification to send to the post office.</param>
        Task SendAsync(string correlationId, DataAvailableNotificationDto dataAvailableNotificationDto);
    }
}
