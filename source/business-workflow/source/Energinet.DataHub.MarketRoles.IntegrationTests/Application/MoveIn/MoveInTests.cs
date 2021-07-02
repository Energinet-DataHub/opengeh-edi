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

using System.Threading.Tasks;
using Energinet.DataHub.MarketRoles.Application.MoveIn;
using Energinet.DataHub.MarketRoles.Domain.Consumers;
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;
using Energinet.DataHub.MarketRoles.Infrastructure.EDIMessaging;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketRoles.IntegrationTests.Application.MoveIn
{
    [IntegrationTest]
    public class MoveInTests : TestHost
    {
        private readonly AccountingPoint _accountingPoint;

        public MoveInTests()
        {
        }

        [Fact]
        public async Task Accept_WhenConsumerIsRegistered_AcceptMessageIsPublished()
        {
            CreateEnergySupplier();
            CreateAccountingPoint();
            SaveChanges();

            var request = CreateRequest();

            var result = await SendRequest(request);

            Assert.True(result.Success);
            await AssertOutboxMessageAsync<PostOfficeEnvelope>(envelope => envelope.MessageType.StartsWith("Confirm"))
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task Accept_WhenEnergySupplierDoesNotExists_IsRejected()
        {
            CreateAccountingPoint();
            SaveChanges();

            var request = CreateRequest();

            var result = await SendRequest(request);

            Assert.False(result.Success);
            await AssertOutboxMessageAsync<PostOfficeEnvelope>(envelope => envelope.MessageType.StartsWith("Reject"))
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task Accept_WhenAccountingPointDoesNotExists_IsRejected()
        {
            CreateEnergySupplier();
            SaveChanges();

            var request = CreateRequest();

            var result = await SendRequest(request);

            Assert.False(result.Success);
            await AssertOutboxMessageAsync<PostOfficeEnvelope>(envelope => envelope.MessageType.StartsWith("Reject"))
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task Accept_WhenConsumerIsRegisteredBySSN_ConsumerIsRegistered()
        {
            CreateEnergySupplier();
            CreateAccountingPoint();
            SaveChanges();

            var request = CreateRequest();
            await SendRequest(request);

            var consumer = await GetService<IConsumerRepository>().GetBySSNAsync(CprNumber.Create(request.SocialSecurityNumber));
            Assert.NotNull(consumer);
        }

        [Fact]
        public async Task Accept_WhenConsumerIsRegisteredByVAT_ConsumerIsRegistered()
        {
            CreateEnergySupplier();
            CreateAccountingPoint();
            SaveChanges();

            var request = CreateRequest(false);
            await SendRequest(request);

            var consumer = await GetService<IConsumerRepository>().GetByVATNumberAsync(CvrNumber.Create(request.VATNumber));
            Assert.NotNull(consumer);
        }

        private RequestMoveIn CreateRequest(bool registerConsumerBySSN = true)
        {
            var consumerSsn = SampleData.ConsumerSSN;
            var moveInDate = GetService<ISystemDateTimeProvider>().Now();
            return new RequestMoveIn(
                SampleData.Transaction,
                SampleData.GlnNumber,
                registerConsumerBySSN ? consumerSsn : string.Empty,
                registerConsumerBySSN == false ? SampleData.ConsumerVAT : string.Empty,
                SampleData.ConsumerName,
                SampleData.GsrnNumber,
                SampleData.MoveInDate);
        }
    }
}
