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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Processing;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Processing.ConsumerDetails;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Processing.EndOfSupplyNotification;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Processing.MeteringPointDetails;
using Energinet.DataHub.MarketRoles.Domain.Consumers;
using Energinet.DataHub.MarketRoles.Domain.EnergySuppliers;
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints;
using Energinet.DataHub.MarketRoles.Infrastructure.DataAccess;
using Energinet.DataHub.MarketRoles.Infrastructure.Integration.IntegrationEvents.EnergySupplierChange;
using Energinet.DataHub.MarketRoles.Infrastructure.Integration.IntegrationEvents.FutureEnergySupplierChangeRegistered;
using MediatR;
using Microsoft.Data.SqlClient;
using NodaTime;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketRoles.IntegrationTests.Application.ChangeOfSupplier.Processing.Commands
{
    [IntegrationTest]
    public class ChangeSupplierTests : TestHost
    {
        private readonly AccountingPoint _accountingPoint;
        private readonly EnergySupplier _energySupplier;
        private readonly EnergySupplier _newEnergySupplier;
        private readonly Consumer _consumer;
        private readonly IMediator _mediator;

        private Transaction _transaction = CreateTransaction();
        private readonly string _glnNumber = "7495563456235";

        public ChangeSupplierTests(DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
            _accountingPoint = CreateAccountingPoint();
            _energySupplier = CreateEnergySupplier(Guid.NewGuid(), SampleData.GsrnNumber);
            _newEnergySupplier = CreateEnergySupplier(Guid.NewGuid(), _glnNumber);
            _consumer = CreateConsumer();
            _mediator = GetService<IMediator>();
        }

        [Fact]
        public async Task ChangeSupplier_WhenEffectiveDateIsDue_IsSuccessful()
        {
            await SimulateProcess().ConfigureAwait(false);

            var command = new ChangeSupplier(_accountingPoint.Id.Value, _transaction.Value);
            await GetService<IMediator>().Send(command, CancellationToken.None).ConfigureAwait(false);

            string query = @"SELECT Count(1) FROM SupplierRegistrations WHERE AccountingPointId = @AccountingPointId AND StartOfSupplyDate IS NOT NULL AND EndOfSupplyDate IS NULL";
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
            await using var sqlCommand = new SqlCommand(query, GetSqlDbConnection());
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task

            sqlCommand.Parameters.Add(new SqlParameter("@AccountingPointId", _accountingPoint.Id.Value));
            sqlCommand.Parameters.Add(new SqlParameter("@EnergySupplierId", _energySupplier.EnergySupplierId.Value));

            var result = await sqlCommand.ExecuteScalarAsync().ConfigureAwait(false);

            Assert.Equal(1, result);
        }

        [Fact]
        public async Task RequestChangeOfSupplier_IsSuccessful_FutureSupplier_IntegrationEventsIsPublished()
        {
            _transaction = CreateTransaction();
            await RequestFutureChangeOfSupplierProcess().ConfigureAwait(false);

            AssertOutboxMessage<FutureEnergySupplierChangeRegisteredIntegrationEvent>();
        }

        private async Task SimulateProcess()
        {
            await SetConsumerMovedIn().ConfigureAwait(false);

            _transaction = CreateTransaction();
            await RequestChangeOfSupplier().ConfigureAwait(false);

            var businessProcessId = GetBusinessProcessId(_transaction);

            await _mediator.Send(new ForwardMeteringPointDetails(_accountingPoint.Id.Value, businessProcessId.Value, _transaction.Value)).ConfigureAwait(false);
            await _mediator.Send(new ForwardConsumerDetails(_accountingPoint.Id.Value, businessProcessId.Value, _transaction.Value)).ConfigureAwait(false);
            await _mediator.Send(new NotifyCurrentSupplier(_accountingPoint.Id.Value, businessProcessId.Value, _transaction.Value)).ConfigureAwait(false);
        }

        private async Task RequestFutureChangeOfSupplierProcess()
        {
            await SetConsumerMovedIn().ConfigureAwait(false);
            await RequestChangeOfSupplierInFuture().ConfigureAwait(false);
        }

        private async Task RequestChangeOfSupplier()
        {
            await _mediator.Send(new RequestChangeOfSupplier(
                _transaction.Value,
                _glnNumber,
                _consumer.CprNumber?.Value ?? throw new InvalidOperationException("CprNumber was supposed to have a value"),
                string.Empty,
                _accountingPoint.GsrnNumber.Value,
                Instant.FromDateTimeUtc(DateTime.UtcNow.AddHours(1)).ToString())).ConfigureAwait(false);
        }

        private async Task RequestChangeOfSupplierInFuture()
        {
            await _mediator.Send(new RequestChangeOfSupplier(
                _transaction.Value,
                _newEnergySupplier.GlnNumber.Value,
                _consumer.CprNumber?.Value ?? throw new InvalidOperationException("CprNumber was supposed to have a value"),
                string.Empty,
                _accountingPoint.GsrnNumber.Value,
                Instant.FromDateTimeUtc(DateTime.UtcNow.AddHours(80)).ToString())).ConfigureAwait(false);
        }

        private async Task SetConsumerMovedIn()
        {
            SetConsumerMovedIn(_accountingPoint, _consumer.ConsumerId, _energySupplier.EnergySupplierId);
            await GetService<IUnitOfWork>().CommitAsync().ConfigureAwait(false);
        }
    }
}
