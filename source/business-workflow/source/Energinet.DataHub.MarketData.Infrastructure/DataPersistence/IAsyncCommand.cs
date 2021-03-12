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

using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Energinet.DataHub.MarketData.Infrastructure
{
    /// <summary>
    /// ExecuteNonQuery on the datastore
    /// </summary>
    public interface IAsyncCommand
    {
        /// <summary>
        /// Execute a non query to the database
        /// </summary>
        /// <param name="dbConnection">Active database connection</param>
        /// <param name="dbTransaction">Database transaction</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        Task ExecuteNonQueryAsync(
            DbConnection dbConnection,
            DbTransaction? dbTransaction,
            CancellationToken cancellationToken = default);
    }
}
