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

using System.Text.Json;
using Energinet.DataHub.ProcessManager.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages;

public abstract class EnqueueValidatedMessagesHandlerBase<TAcceptedData, TRejectedData>(ILogger logger) : EnqueueMessagesHandlerBase(logger)
{
    protected override Task EnqueueMessagesAsync(EnqueueMessagesDto enqueueMessagesDto)
    {
        switch (enqueueMessagesDto.MessageType)
        {
            case "Accepted":
                var acceptedData = DeserializeJsonInput<TAcceptedData>(enqueueMessagesDto);
                return EnqueueAcceptedMessagesAsync(acceptedData);

            case "Rejected":
                var rejectedData = DeserializeJsonInput<TRejectedData>(enqueueMessagesDto);
                return EnqueueRejectedMessagesAsync(rejectedData);

            default:
                throw new ArgumentOutOfRangeException(nameof(enqueueMessagesDto.MessageType), enqueueMessagesDto.MessageType, "Unknown message type. Message type should be \"Accepted\" or \"Rejected\".");
        }
    }

    protected abstract Task EnqueueAcceptedMessagesAsync(TAcceptedData acceptedMessagesData);

    protected abstract Task EnqueueRejectedMessagesAsync(TRejectedData rejectedMessagesData);
}
