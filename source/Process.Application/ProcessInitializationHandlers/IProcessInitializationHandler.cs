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

namespace Energinet.DataHub.EDI.Process.Application.ProcessInitializationHandlers;

/// <summary>
/// Responsible for initializing a process based on the incoming process type and process initialization data.
/// </summary>
public interface IProcessInitializationHandler
{
    /// <summary>
    /// Get whether the handler can/should handle the incoming process type (the message subject)
    /// </summary>
    bool CanHandle(string processTypeToInitialize);

    /// <summary>
    /// Process the incoming process initialization data.
    /// </summary>
    Task ProcessAsync(byte[] processInitializationData);
}
