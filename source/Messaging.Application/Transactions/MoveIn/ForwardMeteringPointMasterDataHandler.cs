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
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Messaging.Application.Common;
using Messaging.Application.Configuration;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Application.MasterData;
using Messaging.Application.OutgoingMessages;
using Messaging.Application.OutgoingMessages.AccountingPointCharacteristics;
using Messaging.Domain.OutgoingMessages;
using NodaTime.Extensions;
using Address = Messaging.Application.OutgoingMessages.AccountingPointCharacteristics.Address;
using Series = Messaging.Application.OutgoingMessages.AccountingPointCharacteristics.Series;

namespace Messaging.Application.Transactions.MoveIn;

public class ForwardMeteringPointMasterDataHandler : IRequestHandler<ForwardMeteringPointMasterData, Unit>
{
    private readonly IMoveInTransactionRepository _transactionRepository;
    private readonly IMarketActivityRecordParser _marketActivityRecordParser;
    private readonly IOutgoingMessageStore _outgoingMessageStore;

    public ForwardMeteringPointMasterDataHandler(
        IMoveInTransactionRepository transactionRepository,
        IMarketActivityRecordParser marketActivityRecordParser,
        IOutgoingMessageStore outgoingMessageStore)
    {
        _transactionRepository = transactionRepository;
        _marketActivityRecordParser = marketActivityRecordParser;
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

        transaction.HasForwardedMeteringPointMasterData();
        return await Task.FromResult(Unit.Value).ConfigureAwait(false);
    }

    private static MarketEvaluationPoint CreateMarketEvaluationPointFrom(
        MasterDataContent masterData,
        MoveInTransaction transaction)
    {
        var address = new Address(
            new StreetDetail(
                masterData.Address!.StreetCode,
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

        return new MarketEvaluationPoint(
            new Mrid(transaction.MarketEvaluationPointId, "A10"),
            new Mrid(masterData.MeteringPointResponsible, "A10"),
            masterData.Type!,
            masterData.SettlementMethod!,
            masterData.MeteringMethod!,
            masterData.ConnectionState!,
            masterData.ReadingPeriodicity!,
            masterData.NetSettlementGroup!,
            masterData.ScheduledMeterReadingDate!,
            new Mrid(masterData.GridAreaDetails!.Code, "NDK"),
            new Mrid(masterData.GridAreaDetails.ToCode, "NDK"),
            new Mrid(masterData.GridAreaDetails.FromCode, "NDK"),
            new Mrid(masterData.PowerPlantGsrnNumber!, "A10"),
            new UnitValue(masterData.Capacity!.ToString(CultureInfo.InvariantCulture), "KWT"),
            masterData.ConnectionType!,
            masterData.DisconnectionType!,
            masterData.AssetType!,
            masterData.ProductionObligation.ToString(),
            new UnitValue(masterData.MaximumPower.ToString(CultureInfo.InvariantCulture), "KWH"),
            new UnitValue(masterData.MaximumCurrent.ToString(CultureInfo.InvariantCulture), "AMP"),
            masterData.MeterNumber!,
            new Series(
                masterData.Series!.Product,
                masterData.Series.UnitType),
            new Mrid(transaction.CurrentEnergySupplierId!, "A10"),
            masterData.EffectiveDate.ToUniversalTime().ToInstant(),
            masterData.Address.LocationDescription,
            masterData.Address.GeoInfoReference.ToString(),
            address,
            masterData.Address.IsActualAddress.ToString(),
            string.IsNullOrEmpty(masterData.ParentMarketEvaluationPointId) ? null : new RelatedMarketEvaluationPoint(new Mrid(masterData.ParentMarketEvaluationPointId, "A10"), "description"),
            null);
    }

    private static OutgoingMessage CreateOutgoingMessage(string id, string documentType, string processType, string receiverId, string marketActivityRecordPayload, string reasonCode)
    {
        return new OutgoingMessage(
            documentType,
            receiverId,
            Guid.NewGuid().ToString(),
            id,
            processType,
            MarketRoles.EnergySupplier,
            DataHubDetails.IdentificationNumber,
            MarketRoles.MeteringPointAdministrator,
            marketActivityRecordPayload,
            reasonCode);
    }

    private OutgoingMessage AccountingPointCharacteristicsMessageFrom(MasterDataContent masterData, MoveInTransaction transaction)
    {
        var marketEvaluationPoint = CreateMarketEvaluationPointFrom(masterData, transaction);
        var marketActivityRecord = new OutgoingMessages.AccountingPointCharacteristics.MarketActivityRecord(
            Guid.NewGuid().ToString(),
            transaction.TransactionId,
            transaction.EffectiveDate,
            marketEvaluationPoint);

        return CreateOutgoingMessage(
            transaction.TransactionId,
            "AccountingPointCharacteristics",
            "E32",
            transaction.NewEnergySupplierId,
            _marketActivityRecordParser.From(marketActivityRecord),
            "E65");
    }
}
