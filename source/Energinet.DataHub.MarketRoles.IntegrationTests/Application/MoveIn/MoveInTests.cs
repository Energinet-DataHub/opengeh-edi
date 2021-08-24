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
using Energinet.DataHub.MarketRoles.Application.MoveIn;
using Energinet.DataHub.MarketRoles.Domain.Consumers;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;
using Energinet.DataHub.MarketRoles.Infrastructure.EDI;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketRoles.IntegrationTests.Application.MoveIn
{
    [IntegrationTest]
    public class MoveInTests : TestHost
    {
        [Fact]
        public async Task Accept_WhenConsumerIsRegistered_AcceptMessageIsPublished()
        {
            CreateEnergySupplier();
            CreateAccountingPoint();
            SaveChanges();

            var request = CreateRequest();

            var result = await SendRequestAsync(request).ConfigureAwait(false);

            Assert.True(result.Success);
            await AssertOutboxMessageAsync<PostOfficeEnvelope>(envelope => envelope.MessageType.StartsWith("Confirm", StringComparison.Ordinal))
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task Accept_WhenEnergySupplierDoesNotExists_IsRejected()
        {
            CreateAccountingPoint();
            SaveChanges();

            var request = CreateRequest();

            var result = await SendRequestAsync(request).ConfigureAwait(false);

            Assert.False(result.Success);
            await AssertOutboxMessageAsync<PostOfficeEnvelope>(envelope => envelope.MessageType.StartsWith("Reject", StringComparison.Ordinal))
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task Accept_WhenAccountingPointDoesNotExists_IsRejected()
        {
            CreateEnergySupplier();
            SaveChanges();

            var request = CreateRequest();

            var result = await SendRequestAsync(request).ConfigureAwait(false);

            Assert.False(result.Success);
            await AssertOutboxMessageAsync<PostOfficeEnvelope>(envelope => envelope.MessageType.StartsWith("Reject", StringComparison.Ordinal))
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task Accept_WhenConsumerIsRegisteredBySSN_ConsumerIsRegistered()
        {
            CreateEnergySupplier();
            CreateAccountingPoint();
            SaveChanges();

            var request = CreateRequest();
            await SendRequestAsync(request).ConfigureAwait(false);

            var consumer = await GetService<IConsumerRepository>().GetBySSNAsync(CprNumber.Create(request.SocialSecurityNumber)).ConfigureAwait(false);
            Assert.NotNull(consumer);
        }

        [Fact]
        public async Task Accept_WhenConsumerIsRegisteredByVAT_ConsumerIsRegistered()
        {
            CreateEnergySupplier();
            CreateAccountingPoint();
            SaveChanges();

            var request = CreateRequest(false);
            await SendRequestAsync(request).ConfigureAwait(false);

            var consumer = await GetService<IConsumerRepository>().GetByVATNumberAsync(CvrNumber.Create(request.VATNumber)).ConfigureAwait(false);
            Assert.NotNull(consumer);
        }

        [Fact]
        public async Task Move_in_on_top_of_move_in_should_result_in_reject_message()
        {
            CreateEnergySupplier();
            CreateAccountingPoint();
            SaveChanges();

            var request = CreateRequest(false);
            await SendRequestAsync(request).ConfigureAwait(false);
            await SendRequestAsync(request).ConfigureAwait(false);

            AssertOutboxMessage<PostOfficeEnvelope>(envelope => envelope.MessageType.StartsWith("Confirm", StringComparison.Ordinal));
            AssertOutboxMessage<PostOfficeEnvelope>(envelope => envelope.MessageType.StartsWith("Reject", StringComparison.Ordinal));
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
