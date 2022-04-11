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
using B2B.Transactions.Infrastructure.Configuration.Correlation;
using B2B.Transactions.Infrastructure.Serialization;
using Microsoft.Azure.Functions.Worker;

namespace B2B.Transactions.Api.Servicebus;

public class ServiceBusMessageMetadataExtractor
{
    private readonly ICorrelationContext _correlationContext;
    private readonly ISerializer _serializer;

    public ServiceBusMessageMetadataExtractor(ICorrelationContext correlationContext, ISerializer serializer)
    {
        _correlationContext = correlationContext;
        _serializer = serializer;
    }

    public void SetCorrelationId(FunctionContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        context.BindingContext.BindingData.TryGetValue("UserProperties", out var serviceBusMessageMetadata);

        if (serviceBusMessageMetadata is null)
        {
            throw new InvalidOperationException($"Service bus metadata must be specified as User Properties attributes");
        }

        var metadata = _serializer.Deserialize<ServiceBusMessageMetadata>(serviceBusMessageMetadata.ToString() ?? throw new InvalidOperationException());
        _correlationContext.SetId(metadata.CorrelationID ?? throw new InvalidOperationException("Service bus metadata property Correlation-ID is missing"));
    }
}
