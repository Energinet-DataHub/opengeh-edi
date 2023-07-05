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
using Api.Configuration;
using Application.Configuration;
using Application.IncomingMessages.RequestAggregatedMeasureData;
using Domain.Transactions;
using Energinet.DataHub.Edi.Responses.AggregatedMeasureData;
using Energinet.DataHub.Wholesale.Contracts.Events;
using Infrastructure.Configuration.Serialization;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Api.IncomingMessages.EdiInbox;

public class RequestEdiInboxMessageQueueListener
{
    private readonly ILogger<RequestEdiInboxMessageQueueListener> _logger;
    private readonly IMediator _mediator;
    private readonly ISerializer _jsonSerializer;
    private readonly ICorrelationContext _correlationContext;

    public RequestEdiInboxMessageQueueListener(IMediator mediator, ISerializer jsonSerializer, ICorrelationContext correlationContext, ILogger<RequestEdiInboxMessageQueueListener> logger)
    {
        _logger = logger;
        _mediator = mediator;
        _jsonSerializer = jsonSerializer;
        _correlationContext = correlationContext;
    }

    [Function(nameof(RequestEdiInboxMessageQueueListener))]
    public async Task RunAsync(
        [ServiceBusTrigger("%EDI_INBOX_MESSAGE_QUEUE_NAME%", Connection = "SERVICE_BUS_CONNECTION_STRING_FOR_DOMAIN_RELAY_LISTENER")] byte[] data,
        FunctionContext context,
        CancellationToken hostCancellationToken)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (context == null) throw new ArgumentNullException(nameof(context));

        using var cancellationTokenSource =
            CancellationTokenSource.CreateLinkedTokenSource(
                hostCancellationToken,
                context.CancellationToken);

        var cancellationToken = cancellationTokenSource.Token;
        SetCorrelationIdFromServiceBusMessage(context);

        context.BindingContext.BindingData.TryGetValue("RequestId", out var processId);
        ArgumentNullException.ThrowIfNull(processId);
        var command = new AggregatedMeasureDataAccepted(data, Guid.Parse((string)processId));
        await _mediator.Send(command, cancellationToken).ConfigureAwait(false);
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

        _logger.LogInformation("Dequeued service bus message with correlation id: " + _correlationContext.Id ?? string.Empty);
    }
}
