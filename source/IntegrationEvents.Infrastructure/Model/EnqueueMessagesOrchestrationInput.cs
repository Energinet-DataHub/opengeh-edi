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

namespace Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.Model;

/// <summary>
/// An immutable input to start the equeue messages orchestration.
/// </summary>
public sealed record EnqueueMessagesOrchestrationInput(
    string CalculationOrchestrationId,
    string CalculationId, // TODO: What is the correct type? Would have expected Guid but in "protobuf" it is string.
    string CalculationType, // TODO: Use correct type
    long CalculationVersion);
