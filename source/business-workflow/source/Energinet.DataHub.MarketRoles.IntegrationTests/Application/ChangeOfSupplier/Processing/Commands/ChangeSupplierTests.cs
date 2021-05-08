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
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Processing.Commands;
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
    [Collection("IntegrationTest")]
    public class ChangeSupplierTests : TestHost
    {
        private readonly AccountingPoint _accountingPoint;
        private readonly EnergySupplier _energySupplier;
        private readonly Consumer _consumer;

        public ChangeSupplierTests()
        {
            _accountingPoint = CreateAccountingPoint();
            _energySupplier = CreateEnergySupplier();
            _consumer = CreateConsumer();
        }

        [Fact]
        public async Task ChangeSupplier_WhenEffectiveDateIsDue_IsSuccessful()
        {
            SetConsumerMovedIn(_accountingPoint, _consumer.ConsumerId, _energySupplier.EnergySupplierId);
            await GetService<IUnitOfWork>().CommitAsync().ConfigureAwait(false);
            var transaction = CreateTransaction();
            await Mediator.Send(new RequestChangeOfSupplier(
                transaction.Value,
                _energySupplier.GlnNumber.Value,
                _consumer.CprNumber.Value,
                _accountingPoint.GsrnNumber.Value,
                SystemDateTimeProvider.Now()));

            var bususinessProcessId = GetBusinessProcessId(transaction);

            await Mediator.Send(new SendMeteringPointDetails(_accountingPoint.Id.Value, bususinessProcessId.Value, transaction.Value));
            await Mediator.Send(new SendConsumerDetails(_accountingPoint.Id.Value, bususinessProcessId.Value, transaction.Value));
            await Mediator.Send(new NotifyCurrentSupplier(_accountingPoint.Id.Value, bususinessProcessId.Value, transaction.Value));

            var command = new ChangeSupplier(_accountingPoint.Id.Value, transaction.Value);
            await GetService<IMediator>().Send(command, CancellationToken.None);

            var sql =
                $"SELECT Count(1) FROM SupplierRegistrations WHERE EnergySupplierId = '{_energySupplier.EnergySupplierId.Value}' AND StartOfSupplyDate IS NOT NULL AND EndOfSupplyDate IS NULL";
            var sqlCommand = new SqlCommand(sql, GetSqlDbConnection());
            var result = sqlCommand.ExecuteNonQuery();
        }
    }
}
