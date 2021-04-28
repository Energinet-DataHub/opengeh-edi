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

using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Energinet.DataHub.MarketRoles.Application;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier;

namespace Energinet.DataHub.MarketRoles.Infrastructure
{
    /// <summary>
    /// TODO: This implementation is only for keeping a working flow in the RequestChangeOfSupplier workflow
    /// and thus should be re-implemented or deleted when refactoring.
    /// </summary>
    public class ServiceBusProcessingClient : IProcessingClient
    {
        private readonly ServiceBusSender _sender;

        public ServiceBusProcessingClient(
            ServiceBusSender sender)
        {
            _sender = sender;
        }

        public async Task SendAsync(RequestChangeOfSupplier request)
        {
            var bytes = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(request));
            await _sender.SendMessageAsync(new ServiceBusMessage(bytes)).ConfigureAwait(false);
        }
    }
}
