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

using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Energinet.DataHub.MarketData.Infrastructure;
using Energinet.DataHub.MarketData.Infrastructure.DataPersistence;

namespace Energinet.DataHub.MarketData.Tests.UnitOfWork
{
    public class InsertRandomText : IAsyncCommand
    {
        public Task ExecuteNonQueryAsync(
            DbConnection dbConnection,
            DbTransaction? dbTransaction,
            CancellationToken cancellationToken = default)
        {
            var cmd = new CommandDefinition(
                "INSERT INTO TextEntries VALUES (@randomText)",
                new { randomText = Guid.NewGuid().ToString("N") },
                dbTransaction,
                cancellationToken: cancellationToken);

            return dbConnection.ExecuteAsync(cmd);
        }
    }
}
