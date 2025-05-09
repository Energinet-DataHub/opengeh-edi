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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Extensions;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Configuration.Options;
using Energinet.DataHub.ProcessManager.Abstractions.Api.Model;
using Energinet.DataHub.ProcessManager.Client;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.ProcessManager;

/// <summary>
/// Create process manager clients. Can return a mock client if
/// <see cref="IncomingMessagesOptions.AllowMockDependenciesForTests"/> is true.
/// </summary>
internal class ProcessManagerMessageClientFactory(
    IOptions<IncomingMessagesOptions> options,
    IProcessManagerMessageClient processManagerMessageClient) : IProcessManagerMessageClientFactory
{
    private readonly IncomingMessagesOptions _options = options.Value;
    private readonly IProcessManagerMessageClient _processManagerMessageClient = processManagerMessageClient;

    /// <inheritdoc />
    public IProcessManagerMessageClient CreateMessageClient(string actorMessageId)
    {
        if (!_options.AllowMockDependenciesForTests)
            return _processManagerMessageClient;

        var isTestMessage = actorMessageId.IsTestUuid();

        return isTestMessage
            ? new ProcessManagerMessageClientMock()
            : _processManagerMessageClient;
    }

    /// <summary>
    /// A mock implementation of <see cref="IProcessManagerMessageClient"/> that does nothing.
    /// </summary>
    private class ProcessManagerMessageClientMock : IProcessManagerMessageClient
    {
        public Task StartNewOrchestrationInstanceAsync<TInputParameterDto>(
            StartOrchestrationInstanceMessageCommand<TInputParameterDto> command,
            CancellationToken cancellationToken)
                where TInputParameterDto : class, IInputParameterDto
        {
            return Task.CompletedTask;
        }

        public Task NotifyOrchestrationInstanceAsync(
            NotifyOrchestrationInstanceEvent notifyEvent,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task NotifyOrchestrationInstanceAsync<TNotifyDataDto>(
            NotifyOrchestrationInstanceEvent<TNotifyDataDto> notifyEvent,
            CancellationToken cancellationToken)
                where TNotifyDataDto : class, INotifyDataDto
        {
            return Task.CompletedTask;
        }
    }
}
