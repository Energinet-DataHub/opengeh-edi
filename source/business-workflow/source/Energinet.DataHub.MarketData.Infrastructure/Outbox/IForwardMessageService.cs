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
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketData.Infrastructure.Outbox
{
    /// <summary>
    /// Service used to process messages placed in the outbox database table (OutgoingActorMessage)
    /// </summary>
    public interface IForwardMessageService
    {
        /// <summary>
        /// Process all messages by fetching one unprocessed message from the DB at a time,
        /// sending it to the Post Office and then marking it as processed in the DB.
        /// </summary>
        Task ProcessMessagesAsync();
    }
}
