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
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Messaging.Application.Configuration;
using Messaging.Application.MasterData;
using Messaging.Application.OutgoingMessages;
using Messaging.Application.OutgoingMessages.Common;
using Messaging.Domain.Actors;
using Messaging.Domain.MasterData.Dictionaries;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.OutgoingMessages.AccountingPointCharacteristics;
using Messaging.Domain.OutgoingMessages.AccountingPointCharacteristics.MarketEvaluationPointDetails;
using Messaging.Domain.Transactions;
using Messaging.Domain.Transactions.MoveIn;
using NodaTime.Extensions;
using Address = Messaging.Domain.OutgoingMessages.AccountingPointCharacteristics.MarketEvaluationPointDetails.Address;
using Series = Messaging.Domain.OutgoingMessages.AccountingPointCharacteristics.MarketEvaluationPointDetails.Series;

namespace Messaging.Application.Transactions.MoveIn.MasterDataDelivery;

public class ForwardMeteringPointMasterDataHandler : IRequestHandler<ForwardMeteringPointMasterData, Unit>
{
    private readonly IMoveInTransactionRepository _transactionRepository;
    private readonly IMessageRecordParser _messageRecordParser;
    private readonly IOutgoingMessageStore _outgoingMessageStore;

    public ForwardMeteringPointMasterDataHandler(
        IMoveInTransactionRepository transactionRepository,
        IMessageRecordParser messageRecordParser,
        IOutgoingMessageStore outgoingMessageStore)
    {
        _transactionRepository = transactionRepository;
        _messageRecordParser = messageRecordParser;
        _outgoingMessageStore = outgoingMessageStore;
    }

    public async Task<Unit> Handle(ForwardMeteringPointMasterData request, CancellationToken cancellationToken)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        var transaction = _transactionRepository.GetById(request.TransactionId);
        if (transaction is null)
        {
            throw new MoveInException($"Could not find move in transaction '{request.TransactionId}'");
        }

        _outgoingMessageStore.Add(AccountingPointCharacteristicsMessageFrom(request.MasterDataContent, transaction));

        transaction.MarkMeteringPointMasterDataAsSent();
        return await Task.FromResult(Unit.Value).ConfigureAwait(false);
    }

    private static MarketEvaluationPoint CreateMarketEvaluationPointFrom(
        MasterDataContent masterData,
        MoveInTransaction transaction)
    {
        var address = CreateAddress(masterData);

        return new MarketEvaluationPoint(
            new Mrid(transaction.MarketEvaluationPointId, "A10"),
            null,
            MasterDataTranslation.GetTranslationFrom(masterData.Type),
            MasterDataTranslation.GetTranslationFrom(masterData.SettlementMethod),
            MasterDataTranslation.GetTranslationFrom(masterData.MeteringMethod),
            MasterDataTranslation.GetTranslationFrom(masterData.ConnectionState),
            MasterDataTranslation.GetTranslationFrom(masterData.ReadingPeriodicity),
            MasterDataTranslation.GetTranslationFrom(masterData.NetSettlementGroup),
            MasterDataTranslation.TranslateToNextReadingDate(masterData.ScheduledMeterReadingDate),
            new Mrid(masterData.GridAreaDetails.Code, "NDK"),
            null,
            null,
            new Mrid(masterData.PowerPlantGsrnNumber, "A10"),
            new UnitValue(masterData.Capacity.ToString(CultureInfo.InvariantCulture), "KWT"),
            MasterDataTranslation.GetTranslationFrom(masterData.ConnectionType),
            MasterDataTranslation.GetTranslationFrom(masterData.DisconnectionType),
            MasterDataTranslation.GetTranslationFrom(masterData.AssetType),
            masterData.ProductionObligation.ToString(),
            new UnitValue(masterData.MaximumPower.ToString(CultureInfo.InvariantCulture), "KWT"),
            new UnitValue(masterData.MaximumCurrent.ToString(CultureInfo.InvariantCulture), "AMP"),
            masterData.MeterNumber,
            new Series(
                MasterDataTranslation.GetTranslationFrom(masterData.Series.Product),
                MasterDataTranslation.GetTranslationFrom(masterData.Series.UnitType)),
            new Mrid(transaction.CurrentEnergySupplierId!, "A10"),
            masterData.EffectiveDate.ToUniversalTime().ToInstant(),
            masterData.Address.LocationDescription,
            masterData.Address.GeoInfoReference.ToString(),
            address,
            masterData.Address.IsActualAddress.ToString(),
            string.IsNullOrEmpty(masterData.ParentMarketEvaluationPointId) ? null : new RelatedMarketEvaluationPoint(new Mrid(masterData.ParentMarketEvaluationPointId, "A10"), "description"),
            null);
    }

    private static Address CreateAddress(MasterDataContent masterData)
    {
        return new Address(
            new StreetDetail(
                masterData.Address.StreetCode,
                masterData.Address.StreetName,
                masterData.Address.BuildingNumber,
                masterData.Address.Floor,
                masterData.Address.Room),
            new TownDetail(
                masterData.Address.MunicipalityCode.ToString(CultureInfo.InvariantCulture),
                masterData.Address.City,
                masterData.Address.CitySubDivision,
                masterData.Address.CountryCode),
            masterData.Address.PostCode);
    }

    private static OutgoingMessage CreateOutgoingMessage(string id, string processType, string receiverId, string marketActivityRecordPayload)
    {
        return new OutgoingMessage(
            MessageType.AccountingPointCharacteristics,
            ActorNumber.Create(receiverId),
            TransactionId.Create(id),
            processType,
            MarketRole.EnergySupplier,
            DataHubDetails.IdentificationNumber,
            MarketRole.MeteringPointAdministrator,
            marketActivityRecordPayload);
    }

    private OutgoingMessage AccountingPointCharacteristicsMessageFrom(MasterDataContent masterData, MoveInTransaction transaction)
    {
        var marketEvaluationPoint = CreateMarketEvaluationPointFrom(masterData, transaction);
        var marketActivityRecord = new MarketActivityRecord(
            Guid.NewGuid().ToString(),
            null,
            transaction.EffectiveDate,
            marketEvaluationPoint);

        return CreateOutgoingMessage(
            transaction.StartedByMessageId,
            "E65",
            transaction.NewEnergySupplierId,
            _messageRecordParser.From(marketActivityRecord));
    }
}
