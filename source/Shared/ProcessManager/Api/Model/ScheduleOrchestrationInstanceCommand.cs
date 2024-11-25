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
/// Command for scheduling an orchestration instance.
/// Must be JSON serializable.
/// </summary>
/// <typeparam name="TInputParameterDto">Must be a JSON serializable type.</typeparam>
public record ScheduleOrchestrationInstanceCommand<TInputParameterDto>
    : UserCommand
    where TInputParameterDto : IInputParameterDto
{
    /// <summary>
    /// Construct command.
    /// </summary>
    /// <param name="operatingIdentity">Identity of the user executing the command.</param>
    /// <param name="runAt">The time when the orchestration instance should be executed by the Scheduler.</param>
    /// <param name="inputParameter">Contains the Durable Functions orchestration input parameter value.</param>
    public ScheduleOrchestrationInstanceCommand(
        UserIdentityDto operatingIdentity,
        DateTimeOffset runAt,
        TInputParameterDto inputParameter)
            : base(operatingIdentity)
    {
        RunAt = runAt;
        InputParameter = inputParameter;
    }

    /// <summary>
    /// The time when the orchestration instance should be executed by the Scheduler.
    /// </summary>
    public DateTimeOffset RunAt { get; }

    /// <summary>
    /// Contains the Durable Functions orchestration input parameter value.
    /// </summary>
    public TInputParameterDto InputParameter { get; }
}
