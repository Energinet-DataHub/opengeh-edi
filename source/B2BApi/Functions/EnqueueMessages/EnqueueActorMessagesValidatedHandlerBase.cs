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

using Energinet.DataHub.ProcessManager.Abstractions.Contracts;
using Energinet.DataHub.ProcessManager.Components.Abstractions.EnqueueActorMessages;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages;

public abstract class EnqueueActorMessagesValidatedHandlerBase<TAcceptedData, TRejectedData>(ILogger logger)
    : EnqueueActorMessagesHandlerBase(logger)
        where TAcceptedData : IEnqueueAcceptedDataDto
        where TRejectedData : IEnqueueRejectedDataDto
{
    protected override Task EnqueueActorMessagesV1Async(
        Guid serviceBusMessageId,
        Guid orchestrationInstanceId,
        EnqueueActorMessagesV1 enqueueActorMessages,
        CancellationToken cancellationToken)
    {
        if (enqueueActorMessages.DataType == typeof(TAcceptedData).Name)
        {
            var acceptedData = enqueueActorMessages.ParseData<TAcceptedData>();
            return EnqueueAcceptedMessagesAsync(
                serviceBusMessageId,
                orchestrationInstanceId,
                acceptedData,
                cancellationToken);
        }
        else if (enqueueActorMessages.DataType == typeof(TRejectedData).Name)
        {
            var rejectedData = enqueueActorMessages.ParseData<TRejectedData>();
            return EnqueueRejectedMessagesAsync(
                serviceBusMessageId,
                orchestrationInstanceId,
                enqueueActorMessages.OrchestrationStartedByActor,
                rejectedData,
                cancellationToken);
        }

        throw new ArgumentOutOfRangeException(
            nameof(enqueueActorMessages.DataType),
            enqueueActorMessages.DataType,
            $"{nameof(EnqueueActorMessagesV1)} contains an invalid data type (expected {typeof(TAcceptedData).Name} or {typeof(TRejectedData).Name}).");
    }

    protected abstract Task EnqueueAcceptedMessagesAsync(
        Guid serviceBusMessageId,
        Guid orchestrationInstanceId,
        TAcceptedData acceptedData,
        CancellationToken cancellationToken);

    protected abstract Task EnqueueRejectedMessagesAsync(
        Guid serviceBusMessageId,
        Guid orchestrationInstanceId,
        EnqueueActorMessagesActorV1 orchestrationStartedByActor,
        TRejectedData rejectedData,
        CancellationToken cancellationToken);
}
