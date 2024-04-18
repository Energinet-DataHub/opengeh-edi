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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain;

/// <summary>
/// Manages the current execution context of the system, primarily used for setting and retrieving the execution type.
/// </summary>
public class ExecutionContext
{
    private ExecutionType? _executionType;

    public ExecutionType? CurrentExecutionType => _executionType;

    /// <summary>
    /// Set the current Execution Context name
    /// </summary>
    public void SetExecutionType(ExecutionType executionType)
    {
        _executionType = executionType;
    }
}
