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
/// A orchestration instance command executed by an identity.
/// </summary>
/// <param name="OperatingIdentity">The identity executing the command.</param>
public abstract record OrchestrationInstanceCommand(
    IOperatingIdentityDto OperatingIdentity);

#pragma warning disable SA1201 // Elements should appear in the correct order
public interface IOrchestratingInstanceCommand
#pragma warning restore SA1201 // Elements should appear in the correct order
{
    IOperatingIdentityDto OperatingIdentity { get; }
}

#pragma warning disable SA1201 // Elements should appear in the correct order
public interface IUserCommand : IOrchestratingInstanceCommand
#pragma warning restore SA1201 // Elements should appear in the correct order
{
    new UserIdentityDto OperatingIdentity { get; }
}
