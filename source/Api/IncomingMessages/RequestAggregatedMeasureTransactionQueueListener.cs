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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.Api.Common;
using Energinet.DataHub.EDI.Api.Configuration;
using Energinet.DataHub.EDI.Application.Configuration;
using Energinet.DataHub.EDI.Application.IncomingMessages.RequestAggregatedMeasureData;
using Energinet.DataHub.EDI.Infrastructure.Configuration.Serialization;
using Energinet.DataHub.EDI.MarketTransactions;
using MediatR;
using Microsoft.Azure.Functions.Worker;

namespace Energinet.DataHub.EDI.Api.IncomingMessages;

public class RequestAggregatedMeasureTransactionQueueListener
{
    private readonly IMediator _mediator;
    private readonly ISerializer _jsonSerializer;
    private readonly ICorrelationContext _correlationContext;

    public RequestAggregatedMeasureTransactionQueueListener(IMediator mediator, ISerializer jsonSerializer, ICorrelationContext correlationContext)
    {
        _mediator = mediator;
        _jsonSerializer = jsonSerializer;
        _correlationContext = correlationContext;
    }

    [Function(nameof(RequestAggregatedMeasureTransactionQueueListener))]
    public async Task RunAsync(
        [ServiceBusTrigger("%INCOMING_AGGREGATED_MEASURE_DATA_QUEUE_NAME%", Connection = "SERVICE_BUS_CONNECTION_STRING_FOR_DOMAIN_RELAY_LISTENER")] byte[] data,
        FunctionContext context,
        CancellationToken hostCancellationToken)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (context == null) throw new ArgumentNullException(nameof(context));

        var cancellationToken = context.CreateCancellationToken(hostCancellationToken);
        SetCorrelationIdFromServiceBusMessage(context);

        var byteAsString = Encoding.UTF8.GetString(data);
        var marketTransaction =
            _jsonSerializer.Deserialize<RequestAggregatedMeasureDataMarketTransaction>(byteAsString);
        await _mediator.Send(
                new RequestAggregatedMeasureDataTransactionCommand(marketTransaction.MessageHeader, marketTransaction.MarketActivityRecord), cancellationToken)
            .ConfigureAwait(false);
    }

    private void SetCorrelationIdFromServiceBusMessage(FunctionContext context)
    {
        context.BindingContext.BindingData.TryGetValue("UserProperties", out var serviceBusMessageMetadata);

        if (serviceBusMessageMetadata is null)
        {
            throw new InvalidOperationException($"Service bus metadata must be specified as User Properties attributes");
        }

        var metadata = _jsonSerializer.Deserialize<ServiceBusMessageMetadata>(serviceBusMessageMetadata.ToString() ?? throw new InvalidOperationException());
        _correlationContext.SetId(metadata.CorrelationID ?? throw new InvalidOperationException("Service bus metadata property CorrelationID is missing"));
    }
}
