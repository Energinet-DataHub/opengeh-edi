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

using Energinet.DataHub.ProcessManager.Api.Model.OrchestrationInstance;

namespace Energinet.DataHub.ProcessManager.Api.Model;

/// <summary>
/// Command for starting an orchestration instance for a message.
/// Must be JSON serializable.
/// </summary>
/// <typeparam name="TInputParameterDto">Must be a JSON serializable type.</typeparam>
public record MessageCommand<TInputParameterDto>
    : StartOrchestrationInstanceCommand<TInputParameterDto>
    where TInputParameterDto : IInputParameterDto
{
    /// <summary>
    /// Construct command.
    /// </summary>
    /// <param name="operatingIdentity">Identity executing the command.</param>
    /// <param name="inputParameter">Contains the Durable Functions orchestration input parameter value.</param>
    /// <param name="messageId">Id of the message that casued this command to be executed.</param>
    public MessageCommand(
        IOperatingIdentityDto operatingIdentity,
        TInputParameterDto inputParameter,
        string messageId)
            : base(operatingIdentity, inputParameter)
    {
        MessageId = messageId;
    }

    /// <summary>
    /// Id of the message that casued this command to be executed.
    /// </summary>
    public string MessageId { get; }
}
