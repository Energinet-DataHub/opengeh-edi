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

using System.Threading;
using System.Threading.Tasks;

namespace Energinet.DataHub.MarketData.Infrastructure.DataPersistence
{
    /// <summary>
    /// Interact with our datastore
    /// </summary>
    public interface IUnitOfWork
    {
        /// <summary>
        /// Execute a non query on the datastore
        /// </summary>
        /// <param name="command">Command to execute</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        Task ExecuteAsync(IAsyncCommand command, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes a query on the datastore
        /// </summary>
        /// <param name="query">Query to execute</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <typeparam name="T">query return type</typeparam>
        /// <returns>query result</returns>
        Task<T> QueryAsync<T>(IAsyncQuery<T> query, CancellationToken cancellationToken = default);
    }
}
