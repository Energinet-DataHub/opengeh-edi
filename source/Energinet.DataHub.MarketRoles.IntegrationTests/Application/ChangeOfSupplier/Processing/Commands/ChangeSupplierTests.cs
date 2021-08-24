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
        private readonly Consumer _consumer;
        private readonly IMediator _mediator;

        private Transaction _transaction = CreateTransaction();
        private string _glnNumber = "7495563456235";

        public ChangeSupplierTests()
        {
            _accountingPoint = CreateAccountingPoint();
            _energySupplier = CreateEnergySupplier();
            CreateEnergySupplier(Guid.NewGuid(), _glnNumber);
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
            await using var sqlCommand = new SqlCommand(query, GetSqlDbConnection());

            sqlCommand.Parameters.Add(new SqlParameter("@AccountingPointId", _accountingPoint.Id.Value));
            sqlCommand.Parameters.Add(new SqlParameter("@EnergySupplierId", _energySupplier.EnergySupplierId.Value));

            var result = await sqlCommand.ExecuteScalarAsync().ConfigureAwait(false);

            Assert.Equal(1, result);
        }

        private async Task SimulateProcess()
        {
            SetConsumerMovedIn(_accountingPoint, _consumer.ConsumerId, _energySupplier.EnergySupplierId);
            await GetService<IUnitOfWork>().CommitAsync().ConfigureAwait(false);

            _transaction = CreateTransaction();
            await Mediator.Send(new RequestChangeOfSupplier(
                _transaction.Value,
                _glnNumber,
                _consumer.CprNumber?.Value ?? throw new InvalidOperationException("CprNumber was supposed to have a value"),
                string.Empty,
                _accountingPoint.GsrnNumber.Value,
                Instant.FromDateTimeUtc(DateTime.UtcNow.AddHours(1)).ToString())).ConfigureAwait(false);

            var businessProcessId = GetBusinessProcessId(_transaction);

            await Mediator.Send(new ForwardMeteringPointDetails(_accountingPoint.Id.Value, businessProcessId.Value, _transaction.Value)).ConfigureAwait(false);
            await Mediator.Send(new ForwardConsumerDetails(_accountingPoint.Id.Value, businessProcessId.Value, _transaction.Value)).ConfigureAwait(false);
            await Mediator.Send(new NotifyCurrentSupplier(_accountingPoint.Id.Value, businessProcessId.Value, _transaction.Value)).ConfigureAwait(false);
        }
    }
}
