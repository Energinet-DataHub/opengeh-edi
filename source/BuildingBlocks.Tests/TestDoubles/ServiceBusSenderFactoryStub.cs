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

using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Azure;

namespace Energinet.DataHub.EDI.IntegrationTests.TestDoubles;

public sealed class ServiceBusSenderFactoryStub : IAzureClientFactory<ServiceBusSender>
{
    private readonly IList<ServiceBusSenderSpy> _senders = [];

    public ServiceBusSender CreateClient(string name)
    {
        return _senders.First(a => a.QueueOrTopicName.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public void AddSenderSpy(ServiceBusSenderSpy senderSpy)
    {
        _senders.Add(senderSpy);
    }
}