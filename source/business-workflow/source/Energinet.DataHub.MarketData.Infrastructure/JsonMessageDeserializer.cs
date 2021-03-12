﻿// Copyright 2020 Energinet DataHub A/S
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
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using GreenEnergyHub.Json;
using GreenEnergyHub.Messaging;

namespace Energinet.DataHub.MarketData.Infrastructure
{
    public class JsonMessageDeserializer : IHubRehydrator
    {
        private readonly IJsonSerializer _jsonSerializer;

        public JsonMessageDeserializer(IJsonSerializer jsonSerializer)
        {
            _jsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
        }

        public async Task<IHubMessage?> RehydrateAsync(Stream messageStream, Type messageType)
        {
            var message = await _jsonSerializer.DeserializeAsync(messageStream, messageType).ConfigureAwait(false);
            return message as IHubMessage;
        }

        public Task<IEnumerable<IHubMessage>?> RehydrateCollectionAsync(Stream messageStream, Type messageType)
        {
            throw new NotImplementedException();
        }
    }
}
