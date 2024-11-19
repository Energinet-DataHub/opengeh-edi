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

using ApiModel = Energinet.DataHub.ProcessManager.Api.Model.OrchestrationInstance;
using DomainModel = Energinet.DataHub.ProcessManagement.Core.Domain.OrchestrationInstance;

namespace Energinet.DataHub.ProcessManager.Api.Mappers;

#pragma warning disable SA1118 // Parameter should not span multiple lines
internal static class OrchestrationInstanceMapperExtensions
{
    public static ApiModel.OrchestrationInstanceDto MapToDto(
        this DomainModel.OrchestrationInstance entity)
    {
        return new ApiModel.OrchestrationInstanceDto(
            Id: entity.Id.Value,
            Lifecycle: entity.Lifecycle.MapToDto(),
            ParameterValue: entity.ParameterValue.AsExpandoObject(),
            Steps: entity.Steps.Select(step => step.MapToDto()).ToList(),
            CustomState: entity.CustomState.Value);
    }

    public static ApiModel.OrchestrationInstanceLifecycleStateDto MapToDto(
        this DomainModel.OrchestrationInstanceLifecycleState entity)
    {
        return new ApiModel.OrchestrationInstanceLifecycleStateDto(
            State: Enum
                .TryParse<ApiModel.OrchestrationInstanceLifecycleStates>(
                    entity.State.ToString(),
                    ignoreCase: true,
                    out var lifecycleStateResult)
                ? lifecycleStateResult
                : throw new InvalidOperationException($"Invalid State '{entity.State}'; cannot be mapped."),
            TerminationState: Enum
                .TryParse<ApiModel.OrchestrationInstanceTerminationStates>(
                    entity.TerminationState.ToString(),
                    ignoreCase: true,
                    out var terminationStateResult)
                ? terminationStateResult
                : null,
            CreatedAt: entity.CreatedAt.ToDateTimeOffset(),
            ScheduledToRunAt: entity.ScheduledToRunAt?.ToDateTimeOffset(),
            QueuedAt: entity.QueuedAt?.ToDateTimeOffset(),
            StartedAt: entity.StartedAt?.ToDateTimeOffset(),
            TerminatedAt: entity.TerminatedAt?.ToDateTimeOffset());
    }

    public static ApiModel.StepInstanceDto MapToDto(
        this DomainModel.StepInstance entity)
    {
        return new ApiModel.StepInstanceDto(
            Id: entity.Id.Value,
            Lifecycle: entity.Lifecycle.MapToDto(),
            Description: entity.Description,
            Sequence: entity.Sequence,
            CustomState: entity.CustomState.Value);
    }

    public static ApiModel.StepInstanceLifecycleStateDto MapToDto(
        this DomainModel.StepInstanceLifecycleState entity)
    {
        return new ApiModel.StepInstanceLifecycleStateDto(
            State: Enum
                .TryParse<ApiModel.StepInstanceLifecycleStates>(
                    entity.State.ToString(),
                    ignoreCase: true,
                    out var lifecycleStateResult)
                ? lifecycleStateResult
                : throw new InvalidOperationException($"Invalid State '{entity.State}'; cannot be mapped."),
            TerminationState: Enum
                .TryParse<ApiModel.OrchestrationStepTerminationStates>(
                    entity.TerminationState.ToString(),
                    ignoreCase: true,
                    out var terminationStateResult)
                ? terminationStateResult
                : null,
            StartedAt: entity.StartedAt?.ToDateTimeOffset(),
            TerminatedAt: entity.TerminatedAt?.ToDateTimeOffset());
    }

    public static IReadOnlyCollection<ApiModel.OrchestrationInstanceDto> MapToDto(
        this IReadOnlyCollection<DomainModel.OrchestrationInstance> entities)
    {
        return entities
            .Select(instance => instance.MapToDto())
            .ToList();
    }
}
#pragma warning restore SA1118 // Parameter should not span multiple lines
