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
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Messaging.Application.Transactions.MoveIn;
using Messaging.Infrastructure.Transactions.MoveIn;

namespace Messaging.IntegrationTests.TestDoubles;

public class RequestCustomerMasterDataSpy : IRequestCustomerMasterData, IRequestDispatcher
{
    private readonly Dictionary<string, ServiceBusMessage> _dispatchedRequests = new();

    public ServiceBusMessage? GetRequest(string correlationId)
    {
        _dispatchedRequests.TryGetValue(correlationId, out var message);
        return message;
    }

    public Task SendAsync(ServiceBusMessage message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        _dispatchedRequests.Add(message.MessageId, message);
        return Task.CompletedTask;
    }

    public Task RequestMasterDataForAsync(FetchCustomerMasterData fetchCustomerMasterData)
    {
        throw new NotImplementedException();
    }
}
