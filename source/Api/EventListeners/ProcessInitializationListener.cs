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
using System.Threading.Tasks;
using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.Core.Messaging.Communication.Subscriber;
using Energinet.DataHub.EDI.Api.EventListeners;
using Energinet.DataHub.EDI.Common.Serialization;
using Energinet.DataHub.EDI.Process.Application.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Process.Interfaces;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.Api.EventListeners;

public class ProcessInitializationListener
{
    private readonly ILogger<ProcessInitializationListener> _logger;
    private readonly IMediator _mediator;
    private readonly ISerializer _serializer;

    public ProcessInitializationListener(ILogger<ProcessInitializationListener> logger, IMediator mediator, ISerializer serializer)
    {
        _logger = logger;
        _mediator = mediator;
        _serializer = serializer;
    }

    [Function(nameof(ProcessInitializationListener))]
    public async Task RunAsync(
        [ServiceBusTrigger(
            "%INCOMING_PROCESS_QUEUE_NAME%",
            Connection = "SERVICE_BUS_CONNECTION_STRING_FOR_DOMAIN_RELAY_LISTENER")]
        byte[] eventData,
        FunctionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var eventDetails = context.ExtractEventDetails();
        _logger.LogInformation("Integration event details: {EventDetails}", eventDetails);
        //This is a temporary solution, an upcoming PR will pass the eventData to a generic handler.
        //The generic handler will then deserialize the eventData and pass it to the correct handler.
        var marketMessage = _serializer.Deserialize<RequestAggregatedMeasureDataDto>(System.Text.Encoding.UTF8.GetString(eventData));
        await _mediator.Send(new InitializeAggregatedMeasureDataProcessesCommand(marketMessage)).ConfigureAwait(false);
    }
}
