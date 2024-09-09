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
/// Represents a file that has already been archived
/// </summary>
public interface IArchivedFile
{
    /// <summary>
    /// A <see cref="FileStorageReference"/> to the archived file. Is used by market document to reference the existing archived file
    /// </summary>
    FileStorageReference FileStorageReference { get; }

    /// <summary>
    /// A <see cref="Stream"/> with the archived file content. Is used to contain the actual file when peeking.
    /// </summary>
    IArchivedMessageStream Document { get; }
}
