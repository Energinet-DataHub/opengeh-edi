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
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Energinet.DataHub.MarketData.Application.Common;
using MediatR;

namespace Energinet.DataHub.MarketData.Application.ChangeOfSupplier.MasterData
{
    public class GetMeteringPointMasterDataQueryHandler : IRequestHandler<QueryMasterData, MasterDataResult>
    {
        private IDbConnectionFactory _connectionFactory;

        public GetMeteringPointMasterDataQueryHandler(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        private IDbConnection Connection => _connectionFactory.GetOpenConnection();

        public Task<MasterDataResult> Handle(QueryMasterData query, CancellationToken cancellationToken)
        {
            return GetMeteringPointMasterDataAsync(query.GsrnNumber ?? throw new ArgumentNullException(query.GsrnNumber));
        }

        private async Task<MasterDataResult> GetMeteringPointMasterDataAsync(string gsrnNumber)
        {
            if (gsrnNumber is null)
            {
                throw new ArgumentNullException(nameof(gsrnNumber));
            }

            var meteringPointQuery =
                $"SELECT * FROM [dbo].[MarketEvaluationPoints]" +
                "WHERE GsrnNumber = @GsrnNumber";

            return await Connection.QueryFirstOrDefaultAsync<MasterDataResult>(
                    meteringPointQuery,
                    new
                    {
                        GsrnNumber = gsrnNumber,
                    }).ConfigureAwait(false);
        }
    }
}
