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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;

public static class DataHubDetails
{
    public static ActorNumber DataHubActorNumber => ActorNumber.Create("5790001330552");

    /// <summary>
    /// System operator that owns Energinet charges
    /// </summary>
    public static ActorNumber SystemOperatorActorNumber => ActorNumber.Create("5790000432752");
}
