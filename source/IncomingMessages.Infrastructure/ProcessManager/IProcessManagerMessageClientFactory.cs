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
using Energinet.DataHub.ProcessManager.Client;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.ProcessManager;

public interface IProcessManagerMessageClientFactory
{
    /// <summary>
    /// Create a process manager message client.
    /// </summary>
    /// <remarks>
    /// If <see cref="IncomingMessagesOptions.AllowMockDependenciesForTests"/> is true, and the
    /// given <paramref name="actorMessageId"/> is a test id, a mock client will be returned. See
    /// <see cref="TestMessageIdExtensions"/> for more information about test id's.
    /// </remarks>
    /// <param name="actorMessageId">The id of the actor message. If it is a test id, a mock will be returned.</param>
    IProcessManagerMessageClient CreateMessageClient(string actorMessageId);
}
