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

using System.Threading.Tasks;

namespace Messaging.Infrastructure.Transactions.MoveIn;

/// <summary>
/// Interface for a request dispatcher
/// </summary>
/// <typeparam name="T">Type of request to dispatch</typeparam>
public interface IRequestDispatcher<in T>
{
    /// <summary>
    /// Dispatch a request async
    /// </summary>
    /// <param name="fetchMeteringPointMasterData"></param>
    /// <returns><see cref="Task"/></returns>
    public Task SendAsync(T fetchMeteringPointMasterData);
}
