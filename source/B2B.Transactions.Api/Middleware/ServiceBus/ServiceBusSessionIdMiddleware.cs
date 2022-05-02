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
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using B2B.Transactions.Infrastructure.Configuration;
using B2B.Transactions.Infrastructure.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace B2B.Transactions.Api.Middleware.ServiceBus
{
    public class ServiceBusSessionIdMiddleware : IFunctionsWorkerMiddleware
    {
        private readonly ISessionContext _sessionContext;
        private readonly ISerializer _serializer;

        public ServiceBusSessionIdMiddleware(ISessionContext sessionContext, ISerializer serializer)
        {
            _sessionContext = sessionContext;
            _serializer = serializer;
        }

        public async Task Invoke(FunctionContext context, [NotNull] FunctionExecutionDelegate next)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            if (context.Is(FunctionContextExtensions.TriggerType.ServiceBusTrigger))
            {
                if (context.BindingContext.BindingData.TryGetValue("MessageSession", out var value) && value is string session)
                {
                    var sessionData = _serializer.Deserialize<Dictionary<string, object>>(session) ?? throw new InvalidOperationException();

                    if (sessionData["SessionId"] is not string sessionId)
                    {
                        throw new InvalidOperationException("Session id does not exist in session data");
                    }

                    _sessionContext.SetId(sessionId);
                }
            }

            await next(context).ConfigureAwait(false);
        }
    }
}
