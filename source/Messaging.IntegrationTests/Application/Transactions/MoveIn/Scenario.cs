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
using System.Threading.Tasks;
using MediatR;
using Messaging.Application.Actors;
using Messaging.Domain.Actors;
using Messaging.Domain.MasterData.MarketEvaluationPoints;
using Messaging.Domain.Transactions;
using Messaging.Domain.Transactions.MoveIn;
using Messaging.Infrastructure.Configuration.DataAccess;
using NodaTime;

namespace Messaging.IntegrationTests.Application.Transactions.MoveIn;

public class Scenario
{
    private static MoveInTransaction? _transaction;
    private readonly IMediator _mediator;
    private readonly B2BContext _context;
    private string? _gridOperatorNumber;
    private Guid _gridOperatorId;
    private CustomerMasterData? _customerMasterData;

    private Scenario(
        MoveInTransaction transaction,
        IMediator mediator,
        B2BContext context)
    {
        _transaction = transaction;
        _mediator = mediator;
        _context = context;
    }

    public static Scenario Details(
        string transactionId,
        string meteringPointNumber,
        Instant supplyStart,
        string currentEnergySupplierNumber,
        string newEnergySupplierNumber,
        string consumerId,
        string consumerIdType,
        string consumerName,
        string startedByMessageId,
        IMediator mediator,
        B2BContext context)
    {
        _transaction = new MoveInTransaction(
            TransactionId.Create(transactionId),
            meteringPointNumber,
            supplyStart,
            currentEnergySupplierNumber,
            startedByMessageId,
            newEnergySupplierNumber,
            consumerId,
            consumerName,
            consumerIdType,
            ActorNumber.Create(newEnergySupplierNumber));

        return new Scenario(_transaction, mediator, context);
    }

    public Scenario IsEffective()
    {
        _transaction?.Accept("FakeId");
        _transaction?.BusinessProcessCompleted();
        _transaction?.MarkMeteringPointMasterDataAsSent();
        return this;
    }

    public Scenario WithGridOperatorForMeteringPoint(Guid actorId, string actorNumber)
    {
        _gridOperatorId = actorId;
        _gridOperatorNumber = actorNumber;
        return this;
    }

    public Scenario CustomerMasterDataIsReceived(
        string marketEvaluationPoint,
        bool electricalHeating,
        Instant? electricalHeatingStart,
        string firstCustomerId,
        string firstCustomerName,
        string secondCustomerId,
        string secondCustomerName,
        bool protectedName,
        bool hasEnergySupplier,
        Instant supplyStart)
    {
        _customerMasterData = new CustomerMasterData(
            marketEvaluationPoint,
            electricalHeating,
            electricalHeatingStart,
            firstCustomerId,
            firstCustomerName,
            secondCustomerId,
            secondCustomerName,
            protectedName,
            hasEnergySupplier,
            supplyStart);
        return this;
    }

    public async Task BuildAsync()
    {
        if (_customerMasterData is not null)
        {
            _transaction!.SetCurrentKnownCustomerMasterData(_customerMasterData);
        }

        await CreateGridOperatorDetailsAsync().ConfigureAwait(false);
        _context.Transactions.Add(_transaction!);
        await _context.SaveChangesAsync();
    }

    private async Task CreateGridOperatorDetailsAsync()
    {
        if (_gridOperatorId != Guid.Empty && !string.IsNullOrEmpty(_gridOperatorNumber))
        {
            var marketEvaluationPoint = new MarketEvaluationPoint(
                Guid.NewGuid(),
                _transaction?.MarketEvaluationPointId!);
            marketEvaluationPoint.SetGridOperatorId(_gridOperatorId);
            _context.MarketEvaluationPoints.Add(marketEvaluationPoint);

            await _mediator.Send(new CreateActor(_gridOperatorId.ToString(), Guid.NewGuid().ToString(), _gridOperatorNumber))
                .ConfigureAwait(false);
        }
    }
}
