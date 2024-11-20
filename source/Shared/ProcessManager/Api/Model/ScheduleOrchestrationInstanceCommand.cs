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
/// </summary>
/// <typeparam name="TInputParameterDto">Must be a JSON serializable type.</typeparam>
/// <param name="UserIdentity">Identity of the user executing the command.</param>
/// <param name="RunAt">The time when the orchestration instance should be executed by the Scheduler.</param>
/// <param name="InputParameter">Contains the Durable Functions orchestration input parameter value.</param>
public record ScheduleOrchestrationInstanceCommand<TInputParameterDto>(
    UserIdentityDto UserIdentity,
    DateTimeOffset RunAt,
    TInputParameterDto InputParameter)
        : UserCommand(UserIdentity)
        where TInputParameterDto : IInputParameterDto;

public record ScheduleOrchestrationInstanceCommandV2<TInputParameterDto>
    : IUserCommand
    where TInputParameterDto : IInputParameterDto
{
    private readonly UserIdentityDto _userIdentity;

    public ScheduleOrchestrationInstanceCommandV2(
        UserIdentityDto userIdentity,
        DateTimeOffset runAt,
        TInputParameterDto inputParameter)
    {
        _userIdentity = userIdentity;

        RunAt = runAt;
        InputParameter = inputParameter;
    }

    public UserIdentityDto OperatingIdentity => _userIdentity;

    public DateTimeOffset RunAt { get; }

    public TInputParameterDto InputParameter { get; }

    IOperatingIdentityDto IOrchestratingInstanceCommand.OperatingIdentity => _userIdentity;
}

public record ScheduleOrchestrationInstanceCommandV3<TInputParameterDto>
    : UserCommand
    where TInputParameterDto : IInputParameterDto
{
    public ScheduleOrchestrationInstanceCommandV3(
        UserIdentityDto userIdentity,
        DateTimeOffset runAt,
        TInputParameterDto inputParameter)
        : base(userIdentity)
    {
        RunAt = runAt;
        InputParameter = inputParameter;
    }

    public DateTimeOffset RunAt { get; }

    public TInputParameterDto InputParameter { get; }
}
