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
using Energinet.DataHub.MeteringPoints.RequestResponse.Response;
using Messaging.Infrastructure.Configuration.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Messaging.Api.IncomingMessages;

public class MeteringPointMasterDataResponseListener
{
    private readonly ILogger<MeteringPointMasterDataResponseListener> _logger;
    private readonly ISerializer _serializer;

    public MeteringPointMasterDataResponseListener(
        ILogger<MeteringPointMasterDataResponseListener> logger,
        ISerializer serializer)
    {
        _logger = logger;
        _serializer = serializer;
    }

    [Function("MeteringPointMasterDataResponseListener")]
    public void Run([ServiceBusTrigger("%METERING_POINT_MASTER_DATA_RESPONSE_QUEUE_NAME%", Connection = "SERVICE_BUS_CONNECTION_STRING_FOR_INTEGRATION_EVENTS_LISTENER")] byte[] data, FunctionContext context)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (context == null) throw new ArgumentNullException(nameof(context));

        var metadata = GetMetaData(context);
        var masterData = MasterDataRequestResponse.Parser.ParseFrom(data);

        _logger.LogInformation($"Master data response received: {data}");
    }

    private MasterDataResponseMetadata GetMetaData(FunctionContext context)
    {
        context.BindingContext.BindingData.TryGetValue("UserProperties", out var metadata);

        if (metadata is null)
        {
            throw new InvalidOperationException($"Service bus metadata must be specified as User Properties attributes");
        }

        return _serializer.Deserialize<MasterDataResponseMetadata>(metadata.ToString() ?? throw new InvalidOperationException());
    }
}
