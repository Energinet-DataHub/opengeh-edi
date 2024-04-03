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

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

/// <summary>
/// Used when a DataHubType can have an unknown value
/// </summary>
public interface IDataHubTypeWithUnknown
{
    /// <summary>
    /// The code of the DataHubType, which can be an unknown value
    /// </summary>
    string Code { get; }

    /// <summary>
    /// The name of the DataHubType
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Whether the DataHubType has an unknown value
    /// </summary>
    bool IsUnknown { get; }
}
