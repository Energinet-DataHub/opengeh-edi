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

using Energinet.DataHub.ProcessManager.Abstractions.Api.Model.OrchestrationDescription;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_024;

// TODO: Move to PM packages in Task #XXX
public static class Brs_024
{
    public const string Name = "Brs_024";

    public static OrchestrationDescriptionUniqueNameDto V1 { get; } = new OrchestrationDescriptionUniqueNameDto(nameof(Brs_024), 1);
}
