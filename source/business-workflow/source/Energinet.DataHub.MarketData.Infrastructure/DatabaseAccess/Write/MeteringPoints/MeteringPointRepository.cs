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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketData.Domain.MeteringPoints;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketData.Infrastructure.DatabaseAccess.Write.MeteringPoints
{
    public class MeteringPointRepository : IMeteringPointRepository
    {
        private readonly IWriteDatabaseContext _writeDatabaseContext;

        public MeteringPointRepository(IWriteDatabaseContext writeDatabaseContext)
        {
            _writeDatabaseContext = writeDatabaseContext;
        }

        public async Task<AccountingPoint> GetByGsrnNumberAsync(GsrnNumber gsrnNumber)
        {
            if (gsrnNumber is null)
            {
                throw new ArgumentNullException(nameof(gsrnNumber));
            }

            var meteringPoint = await _writeDatabaseContext.MarketEvaluationPointDataModels
                .Where(m => m.GsrnNumber == gsrnNumber.Value)
                .FirstOrDefaultAsync();

            return AccountingPoint.CreateFrom(CreateMarketEvaluationPointSnapshot(meteringPoint));
        }

        public void Add(AccountingPoint meteringPoint)
        {
            if (meteringPoint is null)
            {
                throw new ArgumentNullException(nameof(meteringPoint));
            }

            var dataModel = CreateDataModelFrom(meteringPoint);
            _writeDatabaseContext.MarketEvaluationPointDataModels.Add(dataModel);
        }

        public async Task SaveAsync(AccountingPoint meteringPoint)
        {
            if (meteringPoint is null)
            {
                throw new ArgumentNullException(nameof(meteringPoint));
            }

            var dataModel = CreateDataModelFrom(meteringPoint);

            var marketEvaluationPointDataModel = await _writeDatabaseContext.MarketEvaluationPointDataModels.SingleAsync(x => x.Id == meteringPoint.Id);

            marketEvaluationPointDataModel.PhysicalState = dataModel.PhysicalState;
            marketEvaluationPointDataModel.Type = dataModel.Type;
            marketEvaluationPointDataModel.ProductionObligated = dataModel.ProductionObligated;
            marketEvaluationPointDataModel.GsrnNumber = dataModel.GsrnNumber;
            // TODO - Relationships?
        }

        private static MeteringPointDataModel CreateDataModelFrom(AccountingPoint aggregate)
        {
            var snapshot = aggregate.GetSnapshot();

            // TODO - relationships?
            // var relationships = snapshot.Relationships
            //     .Select(r => new RelationshipDataModel(r.Id, marketEvaluationPointId: Guid.Empty, marketParticipantId: snapshot.Id, r.Type, r.EffectuationDate, r.State))
            //     .ToList();
            return new MeteringPointDataModel(snapshot.Id, snapshot.GsrnNumber, snapshot.MeteringPointType, snapshot.IsProductionObligated,  snapshot.PhysicalState, new List<RelationshipDataModel>(), snapshot.Version);
        }

        private static List<RelationshipDataModel> GetAddedRelationships(MeteringPointDataModel meteringPoint)
        {
            return meteringPoint.RelationshipDataModels
                .Where(r => r.Id == default)
                .ToList();
        }

        private static List<RelationshipDataModel> GetChangedRelationships(MeteringPointDataModel meteringPoint)
        {
            return meteringPoint.RelationshipDataModels
                .Where(r => r.Id != default)
                .ToList();
        }

        private static AccountingPointSnapshot CreateMarketEvaluationPointSnapshot(MeteringPointDataModel meteringPointDataModel)
        {
            var meteringPointSnapshot = new AccountingPointSnapshot(
                meteringPointDataModel.Id,
                meteringPointDataModel.GsrnNumber,
                meteringPointDataModel.Type,
                meteringPointDataModel.ProductionObligated,
                meteringPointDataModel.PhysicalState,
                meteringPointDataModel.RowVersion,
                new List<BusinessProcessSnapshot>(),
                new List<ConsumerRegistrationSnapshot>(),
                new List<SupplierRegistrationSnapshot>());

            return meteringPointSnapshot;
        }

        private static MeteringPointDataModel GetDataModelFrom(AccountingPointSnapshot snapshot)
        {
            // TODO - relationships?
            return new MeteringPointDataModel(
                snapshot.Id,
                snapshot.GsrnNumber,
                snapshot.MeteringPointType,
                snapshot.IsProductionObligated,
                snapshot.PhysicalState,
                new List<RelationshipDataModel>(),
                snapshot.Version);
        }

        // private async Task<List<RelationshipDataModel>> GetRelationshipsDataModelAsync(int marketEvaluationPointId)
        // {
        //     var relationshipsQuery =
        //         $"SELECT " +
        //         $"r.Id AS {nameof(RelationshipDataModel.Id)}, " +
        //         $"r.MarketParticipant_Id AS {nameof(RelationshipDataModel.MarketParticipantId)}, " +
        //         $"r.MarketEvaluationPoint_Id AS {nameof(RelationshipDataModel.MarketEvaluationPointId)}, " +
        //         $"r.State AS {nameof(RelationshipDataModel.State)}, " +
        //         $"r.Type AS {nameof(RelationshipDataModel.Type)}, " +
        //         $"r.EffectuationDate AS {nameof(RelationshipDataModel.EffectuationDate)}, " +
        //         $"m.Mrid AS {nameof(RelationshipDataModel.Mrid)} FROM [dbo].[Relationships] r " +
        //         "JOIN [dbo].[MarketParticipants] m ON m.Id = r.MarketParticipant_Id " +
        //         "WHERE MarketEvaluationPoint_Id = @MarketEvaluationPointId";
        //
        //     var result = await Connection.QueryAsync<RelationshipDataModel>(
        //         relationshipsQuery,
        //         new
        //         {
        //             MarketEvaluationPointId = marketEvaluationPointId,
        //         }).ConfigureAwait(false);
        //
        //     return result.ToList();
        // }

        // private async Task InsertAddedRelationshipsAsync(MarketEvaluationPointDataModel marketEvaluationPointDataModel)
        // {
        //     var addedRelationships = GetAddedRelationships(marketEvaluationPointDataModel);
        //
        //     if (addedRelationships.Count == 0)
        //     {
        //         return;
        //     }
        //
        //     var insertStatement =
        //         $"DECLARE @MarketParticipantId NVARCHAR(50) " +
        //         $"SET @MarketParticipantId = (SELECT Id AS MarketParticipantId FROM [dbo].[MarketParticipants] WHERE Mrid = @Mrid)" +
        //         $"INSERT INTO [dbo].[Relationships] (MarketEvaluationPoint_Id, MarketParticipant_Id, Type, EffectuationDate, State) " +
        //         "VALUES (@MarketEvaluationPointId, @MarketParticipantId, @Type, @EffectuationDate, @State)";
        //
        //     var parameters = new List<dynamic>();
        //     addedRelationships.ForEach(relationshipDataModel =>
        //     {
        //         parameters.Add(new
        //         {
        //             Mrid = relationshipDataModel.Mrid,
        //             MarketEvaluationPointId = marketEvaluationPointDataModel.Id,
        //             Type = relationshipDataModel.Type,
        //             EffectuationDate = relationshipDataModel.EffectuationDate,
        //             State = relationshipDataModel.State,
        //         });
        //     });
        //
        //     await Connection.ExecuteAsync(
        //         insertStatement,
        //         parameters).ConfigureAwait(false);
        // }

        // private async Task UpdateChangedRelationshipsAsync(MarketEvaluationPointDataModel marketEvaluationPointDataModel)
        // {
        //     var changedRelationships = GetChangedRelationships(marketEvaluationPointDataModel);
        //
        //     if (changedRelationships.Count == 0)
        //     {
        //         return;
        //     }
        //
        //     var updateStatement =
        //         $"UPDATE [dbo].[Relationships] " +
        //         "SET [EffectuationDate] = @EffectuationDate, [State] = @State " +
        //         "WHERE [Id] = @Id";
        //
        //     var parameters = new List<dynamic>();
        //     marketEvaluationPointDataModel.Relationships?.ForEach(model =>
        //     {
        //         parameters.Add(new
        //         {
        //             Id = model.Id,
        //             EffectuationDate = model.EffectuationDate,
        //             State = model.State,
        //         });
        //     });
        //
        //     await Connection.ExecuteAsync(
        //             updateStatement,
        //             parameters)
        //         .ConfigureAwait(false);
        // }
        private async Task InsertAsync(MeteringPointDataModel dataModel)
        {
            await _writeDatabaseContext.MarketEvaluationPointDataModels.AddAsync(dataModel);
        }
    }
}
