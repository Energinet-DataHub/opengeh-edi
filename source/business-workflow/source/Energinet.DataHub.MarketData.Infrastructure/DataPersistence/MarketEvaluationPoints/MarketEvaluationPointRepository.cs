// // Copyright 2020 Energinet DataHub A/S
// //
// // Licensed under the Apache License, Version 2.0 (the "License2");
// // you may not use this file except in compliance with the License.
// // You may obtain a copy of the License at
// //
// //     http://www.apache.org/licenses/LICENSE-2.0
// //
// // Unless required by applicable law or agreed to in writing, software
// // distributed under the License is distributed on an "AS IS" BASIS,
// // WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// // See the License for the specific language governing permissions and
// // limitations under the License.
//
// using System;
// using System.Collections.Generic;
// using System.Data;
// using System.Linq;
// using System.Threading.Tasks;
// using Dapper;
// using Energinet.DataHub.MarketData.Application.Common;
// using Energinet.DataHub.MarketData.Domain.MeteringPoints;
//
// namespace Energinet.DataHub.MarketData.Infrastructure.DataPersistence.MarketEvaluationPoints
// {
//     public class MeteringPointRepository : IMeteringPointRepository, ICanUpdateDataModel, ICanInsertDataModel
//     {
//         private readonly IUnitOfWorkCallback _unitOfWorkCallback;
//         private readonly IDbConnectionFactory _connectionFactory;
//
//         public MeteringPointRepository(IDbConnectionFactory connectionFactory, IUnitOfWorkCallback unitOfWorkCallback)
//         {
//             _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
//             _unitOfWorkCallback = unitOfWorkCallback ?? throw new ArgumentNullException(nameof(unitOfWorkCallback));
//         }
//
//         private IDbConnection Connection => _connectionFactory.GetOpenConnection();
//
//         public async Task<AccountingPoint> GetByGsrnNumberAsync(GsrnNumber gsrnNumber)
//         {
//             if (gsrnNumber is null)
//             {
//                 throw new ArgumentNullException(nameof(gsrnNumber));
//             }
//
//             var meteringPointQuery =
//                 $"SELECT * FROM [dbo].[MarketEvaluationPoints]" +
//                 "WHERE GsrnNumber = @GsrnNumber";
//
//             var meteringPoint =
//                 await Connection.QueryFirstOrDefaultAsync<MarketEvaluationPointDataModel>(
//                     meteringPointQuery,
//                     new
//                     {
//                         GsrnNumber = gsrnNumber.Value,
//                     }).ConfigureAwait(false);
//
//             if (meteringPoint is null)
//             {
//                 return null!;
//             }
//
//             var relationships = await GetRelationshipsDataModelAsync(meteringPoint.Id).ConfigureAwait(false);
//
//             return AccountingPoint.CreateFrom(CreateMarketEvaluationPointSnapshot(meteringPoint, relationships));
//         }
//
//         public void Add(AccountingPoint accountingPoint)
//         {
//             if (accountingPoint is null)
//             {
//                 throw new ArgumentNullException(nameof(accountingPoint));
//             }
//
//             var dataModel = CreateDataModelFrom(accountingPoint);
//             _unitOfWorkCallback.RegisterNew(dataModel, this);
//         }
//
//         public void Save(AccountingPoint accountingPoint)
//         {
//             if (accountingPoint is null)
//             {
//                 throw new ArgumentNullException(nameof(accountingPoint));
//             }
//
//             var dataModel = CreateDataModelFrom(accountingPoint);
//             _unitOfWorkCallback.RegisterAmended(dataModel, this);
//         }
//
//         public Task PersistCreationOfAsync(IDataModel entity)
//         {
//             if (entity is null)
//             {
//                 throw new ArgumentNullException(nameof(entity));
//             }
//
//             var dataModel = (MarketEvaluationPointDataModel)entity;
//             return InsertAsync(dataModel);
//         }
//
//         public async Task PersistUpdateOfAsync(IDataModel entity)
//         {
//             if (entity is null)
//             {
//                 throw new ArgumentNullException(nameof(entity));
//             }
//
//             var dataModel = (MarketEvaluationPointDataModel)entity;
//
//             await InsertAddedRelationshipsAsync(dataModel).ConfigureAwait(false);
//             await UpdateChangedRelationshipsAsync(dataModel).ConfigureAwait(false);
//             await UpdateRowVersionOrThrowAsync(dataModel).ConfigureAwait(false);
//         }
//
//         private static MarketEvaluationPointDataModel CreateDataModelFrom(AccountingPoint aggregate)
//         {
//             var snapshot = aggregate.GetSnapshot();
//             var relationships = new List<RelationshipDataModel>();
//             // TODO: We must decide on a data model. Until then; use empty list
//             // var relationships = snapshot.BusinessProcesses
//             //     .Select(r => new RelationshipDataModel(r.Id, snapshot.Id, r.MarketParticipantMrid, r.Type, r.EffectuationDate, r.State))
//             //     .ToList();
//             return new MarketEvaluationPointDataModel(snapshot.Id, snapshot.GsrnNumber, snapshot.MeteringPointType, relationships, snapshot.IsProductionObligated, snapshot.PhysicalState, snapshot.Version);
//         }
//
//         private static List<RelationshipDataModel> GetAddedRelationships(MarketEvaluationPointDataModel marketEvaluationPoint)
//         {
//             return marketEvaluationPoint.Relationships
//                 .Where(r => r.Id == default(int))
//                 .ToList();
//         }
//
//         private static List<RelationshipDataModel> GetChangedRelationships(MarketEvaluationPointDataModel marketEvaluationPoint)
//         {
//             return marketEvaluationPoint.Relationships
//                 .Where(r => r.Id != default(int))
//                 .ToList();
//         }
//
//         private static MeteringPointSnapshot CreateMarketEvaluationPointSnapshot(MarketEvaluationPointDataModel marketEvaluationPointDataModel, List<RelationshipDataModel> relationshipDataModels)
//         {
//             var relationshipsSnapshot = relationshipDataModels.Select(r =>
//                     new RelationshipSnapshot(r.Id, r.Mrid!, r.Type, r.EffectuationDate, r.State))
//                 .ToList();
//
//             // TODO: We must decide on data model. Until then, return empty business processes, consumer and supplier registrations
//             // var meteringPointSnapshot = new MeteringPointSnapshot(
//             //     marketEvaluationPointDataModel.Id,
//             //     marketEvaluationPointDataModel.GsrnNumber,
//             //     marketEvaluationPointDataModel.Type,
//             //     relationshipsSnapshot,
//             //     marketEvaluationPointDataModel.ProductionObligated,
//             //     marketEvaluationPointDataModel.PhysicalState,
//             //     marketEvaluationPointDataModel.RowVersion);
//             //return meteringPointSnapshot;
//             var meteringPointSnapshot = new MeteringPointSnapshot(
//                  marketEvaluationPointDataModel.Id,
//                  marketEvaluationPointDataModel.GsrnNumber,
//                  marketEvaluationPointDataModel.Type,
//                  marketEvaluationPointDataModel.ProductionObligated,
//                  marketEvaluationPointDataModel.PhysicalState,
//                  marketEvaluationPointDataModel.RowVersion,
//                  new List<BusinessProcessSnapshot>(),
//                  new List<ConsumerRegistrationSnapshot>(),
//                  new List<SupplierRegistrationSnapshot>());
//             return meteringPointSnapshot;
//         }
//
//         private async Task<List<RelationshipDataModel>> GetRelationshipsDataModelAsync(int marketEvaluationPointId)
//         {
//             var relationshipsQuery =
//                 $"SELECT " +
//                 $"r.Id AS {nameof(RelationshipDataModel.Id)}, " +
//                 $"r.MarketParticipant_Id AS {nameof(RelationshipDataModel.MarketParticipantId)}, " +
//                 $"r.MarketEvaluationPoint_Id AS {nameof(RelationshipDataModel.MarketEvaluationPointId)}, " +
//                 $"r.State AS {nameof(RelationshipDataModel.State)}, " +
//                 $"r.Type AS {nameof(RelationshipDataModel.Type)}, " +
//                 $"r.EffectuationDate AS {nameof(RelationshipDataModel.EffectuationDate)}, " +
//                 $"m.Mrid AS {nameof(RelationshipDataModel.Mrid)} FROM [dbo].[Relationships] r " +
//                 "JOIN [dbo].[MarketParticipants] m ON m.Id = r.MarketParticipant_Id " +
//                 "WHERE MarketEvaluationPoint_Id = @MarketEvaluationPointId";
//
//             var result = await Connection.QueryAsync<RelationshipDataModel>(
//                 relationshipsQuery,
//                 new
//                 {
//                     MarketEvaluationPointId = marketEvaluationPointId,
//                 }).ConfigureAwait(false);
//
//             return result.ToList();
//         }
//
//         private async Task InsertAddedRelationshipsAsync(MarketEvaluationPointDataModel marketEvaluationPointDataModel)
//         {
//             var addedRelationships = GetAddedRelationships(marketEvaluationPointDataModel);
//
//             if (addedRelationships.Count == 0)
//             {
//                 return;
//             }
//
//             var insertStatement =
//                 $"DECLARE @MarketParticipantId NVARCHAR(50) " +
//                 $"SET @MarketParticipantId = (SELECT Id AS MarketParticipantId FROM [dbo].[MarketParticipants] WHERE Mrid = @Mrid)" +
//                 $"INSERT INTO [dbo].[Relationships] (MarketEvaluationPoint_Id, MarketParticipant_Id, Type, EffectuationDate, State) " +
//                 "VALUES (@MarketEvaluationPointId, @MarketParticipantId, @Type, @EffectuationDate, @State)";
//
//             var parameters = new List<dynamic>();
//             addedRelationships.ForEach(relationshipDataModel =>
//             {
//                 parameters.Add(new
//                 {
//                     Mrid = relationshipDataModel.Mrid,
//                     MarketEvaluationPointId = marketEvaluationPointDataModel.Id,
//                     Type = relationshipDataModel.Type,
//                     EffectuationDate = relationshipDataModel.EffectuationDate,
//                     State = relationshipDataModel.State,
//                 });
//             });
//
//             await Connection.ExecuteAsync(
//                 insertStatement,
//                 parameters).ConfigureAwait(false);
//         }
//
//         private async Task UpdateChangedRelationshipsAsync(MarketEvaluationPointDataModel marketEvaluationPointDataModel)
//         {
//             var changedRelationships = GetChangedRelationships(marketEvaluationPointDataModel);
//
//             if (changedRelationships.Count == 0)
//             {
//                 return;
//             }
//
//             var updateStatement =
//                 $"UPDATE [dbo].[Relationships] " +
//                 "SET [EffectuationDate] = @EffectuationDate, [State] = @State " +
//                 "WHERE [Id] = @Id";
//
//             var parameters = new List<dynamic>();
//             marketEvaluationPointDataModel.Relationships?.ForEach(model =>
//             {
//                 parameters.Add(new
//                 {
//                     Id = model.Id,
//                     EffectuationDate = model.EffectuationDate,
//                     State = model.State,
//                 });
//             });
//
//             await Connection.ExecuteAsync(
//                     updateStatement,
//                     parameters)
//                 .ConfigureAwait(false);
//         }
//
//         private async Task UpdateRowVersionOrThrowAsync(MarketEvaluationPointDataModel dataModel)
//         {
//             var updateStatement =
//                 "UPDATE [dbo].[MarketEvaluationPoints] " +
//                 "SET RowVersion = @Version " +
//                 "WHERE Id = @Id AND RowVersion = @PreviousVersion";
//
//             var recordsUpdated = await Connection.ExecuteAsync(
//                 updateStatement,
//                 param: new
//                 {
//                     Id = dataModel.Id,
//                     PreviousVersion = dataModel.RowVersion,
//                     Version = dataModel.RowVersion + 1,
//                 }).ConfigureAwait(false);
//
//             if (recordsUpdated != 1)
//             {
//                 throw new DBConcurrencyException();
//             }
//         }
//
//         private async Task InsertAsync(MarketEvaluationPointDataModel dataModel)
//         {
//             var insertStatement =
//                 $"INSERT INTO [dbo].[MarketEvaluationPoints] (GsrnNumber, Type, ProductionObligated, PhysicalState, RowVersion) " +
//                 "VALUES (@GsrnNumber, @Type, @ProductionObligated, @PhysicalState, @Version);SELECT CAST(SCOPE_IDENTITY() as int)";
//
//             dataModel.Id = await Connection.ExecuteScalarAsync<int>(
//                 insertStatement,
//                 param: new
//                 {
//                     GsrnNumber = dataModel.GsrnNumber,
//                     Type = dataModel.Type,
//                     ProductionObligated = dataModel.ProductionObligated,
//                     PhysicalState = dataModel.PhysicalState,
//                     Version = 0,
//                 }).ConfigureAwait(false);
//
//             await InsertAddedRelationshipsAsync(dataModel).ConfigureAwait(false);
//         }
//     }
// }
using System.Threading.Tasks;
using Energinet.DataHub.MarketData.Domain.MeteringPoints;

public class MarketEvaluationPointRepository : IMeteringPointRepository
{
    public Task<AccountingPoint> GetByGsrnNumberAsync(GsrnNumber gsrnNumber)
    {
        throw new System.NotImplementedException();
    }

    public void Add(AccountingPoint accountingPoint)
    {
        throw new System.NotImplementedException();
    }

    public Task SaveAsync(AccountingPoint accountingPoint)
    {
        throw new System.NotImplementedException();
    }
}
