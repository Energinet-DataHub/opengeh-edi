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
using Messaging.Application.OutgoingMessages;
using Messaging.Application.OutgoingMessages.AccountingPointCharacteristics;
using NodaTime.Extensions;

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

        //TODO: Handle message creation and dispatching
        _outgoingMessageStore.Add(AccountingPointCharacteristicsMessageFrom(request, transaction));

        transaction.HasForwardedMeteringPointMasterData();
        return await Task.FromResult(Unit.Value).ConfigureAwait(false);
    }

    private static MarketEvaluationPoint CreateMarketEvaluationPointFrom(
        ForwardMeteringPointMasterData forwardMeteringPointMasterData)
    {
        var address = new Address(
            new StreetDetail(
                forwardMeteringPointMasterData.MasterDataContent.Address!.StreetCode,
                forwardMeteringPointMasterData.MasterDataContent.Address.StreetName,
                forwardMeteringPointMasterData.MasterDataContent.Address.BuildingNumber,
                forwardMeteringPointMasterData.MasterDataContent.Address.Floor,
                forwardMeteringPointMasterData.MasterDataContent.Address.Room),
            new TownDetail(
                forwardMeteringPointMasterData.MasterDataContent.Address.MunicipalityCode.ToString(CultureInfo.InvariantCulture),
                forwardMeteringPointMasterData.MasterDataContent.Address.City,
                forwardMeteringPointMasterData.MasterDataContent.Address.CitySubDivision,
                forwardMeteringPointMasterData.MasterDataContent.Address.CountryCode),
            forwardMeteringPointMasterData.MasterDataContent.Address.PostCode);

        return new MarketEvaluationPoint(
            new Mrid("id", "codingScheme"),
            new Mrid("id", "codingSceme"),
            forwardMeteringPointMasterData.MasterDataContent.Type!,
            forwardMeteringPointMasterData.MasterDataContent.SettlementMethod!,
            forwardMeteringPointMasterData.MasterDataContent.MeteringMethod!,
            forwardMeteringPointMasterData.MasterDataContent.ConnectionState!,
            "readCycle",
            forwardMeteringPointMasterData.MasterDataContent.NetSettlementGroup!,
            forwardMeteringPointMasterData.MasterDataContent.ScheduledMeterReadingDate!,
            new Mrid(forwardMeteringPointMasterData.MasterDataContent.GridAreaDetails!.Code, "NDK"),
            new Mrid(forwardMeteringPointMasterData.MasterDataContent.GridAreaDetails.ToCode, "NDK"),
            new Mrid(forwardMeteringPointMasterData.MasterDataContent.GridAreaDetails.FromCode, "NDK"),
            new Mrid("Linked_MarketEvaluation_Point", "codingscheme"),
            new UnitValue(forwardMeteringPointMasterData.MasterDataContent.Capacity!.ToString(CultureInfo.InvariantCulture), "KWT"),
            forwardMeteringPointMasterData.MasterDataContent.ConnectionType!,
            forwardMeteringPointMasterData.MasterDataContent.DisconnectionType!,
            "psrType",
            forwardMeteringPointMasterData.MasterDataContent.ProductionObligation.ToString(),
            new UnitValue("contractedConnectionCapacity", "KWH"),
            new UnitValue(forwardMeteringPointMasterData.MasterDataContent.MaximumCurrent.ToString(CultureInfo.InvariantCulture), "AMP"),
            forwardMeteringPointMasterData.MasterDataContent.MeterNumber!,
            new Series(
                forwardMeteringPointMasterData.MasterDataContent.Series!.Product,
                forwardMeteringPointMasterData.MasterDataContent.Series.UnitType),
            new Mrid("EnergySupplier", "A10"),
            forwardMeteringPointMasterData.MasterDataContent.EffectiveDate.ToUniversalTime().ToInstant(),
            forwardMeteringPointMasterData.MasterDataContent.Address.LocationDescription,
            forwardMeteringPointMasterData.MasterDataContent.Address.GeoInfoReference.ToString(),
            address,
            forwardMeteringPointMasterData.MasterDataContent.Address.IsActualAddress.ToString(),
            new RelatedMarketEvaluationPoint(new Mrid("id", "codingScheme"), "description"),
            new RelatedMarketEvaluationPoint(new Mrid("id", "codingScheme"), "description"));
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

    private OutgoingMessage AccountingPointCharacteristicsMessageFrom(ForwardMeteringPointMasterData forwardMeteringPointMasterData, MoveInTransaction transaction)
    {
        var marketEvaluationPoint = CreateMarketEvaluationPointFrom(forwardMeteringPointMasterData);
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
