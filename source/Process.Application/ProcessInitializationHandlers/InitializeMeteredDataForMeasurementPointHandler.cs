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

using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.Process.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.Process.Application.ProcessInitializationHandlers;

public class InitializeMeteredDataForMeasurementPointHandler(
    IMediator mediator,
    ISerializer serializer,
    ILogger<InitializeMeteredDataForMeasurementPointHandler> logger)
    : IProcessInitializationHandler
{
    private readonly IMediator _mediator = mediator;
    private readonly ISerializer _serializer = serializer;
    private readonly ILogger<InitializeMeteredDataForMeasurementPointHandler> _logger = logger;

    public bool CanHandle(string processTypeToInitialize)
    {
        ArgumentNullException.ThrowIfNull(processTypeToInitialize);
        return processTypeToInitialize.Equals(nameof(InitializeMeteredDataForMeasurementPointMessageProcessDto), StringComparison.Ordinal);
    }

    public Task ProcessAsync(byte[] processInitializationData)
    {
        var marketMessage = _serializer.Deserialize<InitializeAggregatedMeasureDataProcessDto>(System.Text.Encoding.UTF8.GetString(processInitializationData));
        _logger.LogInformation("Received InitializeAggregatedMeasureDataProcess for message {MessageId}", marketMessage.MessageId);
        // Nothing to see here yet.
        return Task.CompletedTask;
    }
}