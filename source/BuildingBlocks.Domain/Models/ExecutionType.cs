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

using System;
using System.Linq;

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

/// <summary>
/// Represents different types of execution contexts in the system, primarily used for audit fields.
/// </summary>
public class ExecutionType : EnumerationType
{
    public static readonly ExecutionType TenSecondsHasPassed = new(nameof(TenSecondsHasPassed));
    public static readonly ExecutionType IntegrationEventListener = new(nameof(IntegrationEventListener));
    public static readonly ExecutionType ProcessInitializationListener = new(nameof(ProcessInitializationListener));
    public static readonly ExecutionType InboxEventListener = new(nameof(InboxEventListener));
    public static readonly ExecutionType IncomingMessageReceiver = new(nameof(IncomingMessageReceiver));
    public static readonly ExecutionType PeekRequestListener = new(nameof(PeekRequestListener));
    public static readonly ExecutionType DequeueRequestListener = new(nameof(DequeueRequestListener));
    public static readonly ExecutionType HealthCheck = new(nameof(HealthCheck));
    public static readonly ExecutionType B2CWebApi = new(nameof(B2CWebApi));
    public static readonly ExecutionType Test = new(nameof(Test));

    public ExecutionType(string name)
        : base(name)
    {
    }

    public static ExecutionType FromName(string name)
    {
        return GetAll<ExecutionType>().FirstOrDefault(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
               ?? throw new InvalidOperationException($"{name} is not a valid {typeof(ExecutionType)} {nameof(name)}");
    }
}
