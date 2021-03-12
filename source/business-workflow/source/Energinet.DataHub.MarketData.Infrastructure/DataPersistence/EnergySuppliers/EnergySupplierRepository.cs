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
using System.Threading.Tasks;
using Dapper;
using Energinet.DataHub.MarketData.Application.Common;
using Energinet.DataHub.MarketData.Domain.EnergySuppliers;

namespace Energinet.DataHub.MarketData.Infrastructure.DataPersistence.EnergySuppliers
{
    public class EnergySupplierRepository : IEnergySupplierRepository, ICanInsertDataModel
    {
        private readonly IUnitOfWorkCallback _unitOfWorkCallback;
        private readonly IDbConnectionFactory _connectionFactory;

        public EnergySupplierRepository(IUnitOfWorkCallback unitOfWorkCallback, IDbConnectionFactory connectionFactory)
        {
            _unitOfWorkCallback = unitOfWorkCallback ?? throw new ArgumentNullException(nameof(unitOfWorkCallback));
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        private IDbConnection Connection => _connectionFactory.GetOpenConnection();

        public async Task<bool> ExistsAsync(GlnNumber glnNumber)
        {
            if (glnNumber is null)
            {
                throw new ArgumentNullException(nameof(glnNumber));
            }

            var query = $"SELECT COUNT(1) FROM [dbo].[MarketParticipants] " +
                        "WHERE Mrid = @GlnNumber";

            var exists = await Connection.ExecuteScalarAsync<bool>(query, new
            {
                GlnNumber = glnNumber.Value,
            }).ConfigureAwait(false);

            return exists;
        }

        public void Add(EnergySupplier energySupplier)
        {
            if (energySupplier is null)
            {
                throw new ArgumentNullException(nameof(energySupplier));
            }

            var snapshot = energySupplier.GetSnapshot();
            var dataModel = GetDataModelFrom(snapshot);
            _unitOfWorkCallback.RegisterNew(dataModel, this);
        }

        public async Task PersistCreationOfAsync(IDataModel entity)
        {
            if (entity is null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var dataModel = (EnergySupplierDataModel)entity;

            await Connection.ExecuteAsync(
                $"INSERT INTO [dbo].[MarketParticipants] VALUES(@MrId, @RowVersion);", param: new
                {
                    MrId = dataModel.Mrid,
                    RowVersion = dataModel.RowVersion,
                }).ConfigureAwait(false);
        }

        private static EnergySupplierDataModel GetDataModelFrom(EnergySupplierSnapshot snapshot)
        {
            return new EnergySupplierDataModel(snapshot.Id, snapshot.GlnNumber, snapshot.Version);
        }
    }
}
