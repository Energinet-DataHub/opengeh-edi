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

using Communication.Internal;

namespace Communication;

/// <summary>
/// In order to use the `Communication` library to publish Point to Point an implementation of this interface is required.
/// The implementation is responsible for creating or fetching point to point events (likely from a database)
/// and subsequently commit state changes.
/// </summary>
public interface IPointToPointEventProvider
{
    /// <summary>
    /// Get Point To Point event
    /// </summary>
    /// <returns>A PointToPoint event</returns>
    IAsyncEnumerable<PointToPointEvent> GetAsync();
}
