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

namespace Messaging.PerformanceTest.Actors;

/// <summary>
/// Service for get actor numbers for test
/// </summary>
public interface IActorService
{
    /// <summary>
    /// Get unique actor number for test
    /// </summary>
    string? GetUniqueActorNumber();

    /// <summary>
    /// Get number of actor numbers
    /// </summary>
    /// <returns>Number of actor numbers available</returns>
    int GetActorCount();

    /// <summary>
    /// Check if actor number exists and is in use
    /// </summary>
    /// <param name="actorNumber"></param>
    /// <returns>boolean indicating whether actor number is in use</returns>
    bool IsActorNumberInUse(string actorNumber);
}
