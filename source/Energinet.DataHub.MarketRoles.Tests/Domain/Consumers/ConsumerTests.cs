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
using System.Linq;
using Energinet.DataHub.MarketRoles.Domain.Consumers;
using Energinet.DataHub.MarketRoles.Domain.Consumers.Events;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketRoles.Tests.Domain.Consumers
{
    [UnitTest]
    public class ConsumerTests
    {
        [Fact]
        public void Create_UsingCprNumber_IsSuccessful()
        {
            var cprNumber = CprNumber.Create(SampleData.ConsumerSocialSecurityNumber);
            var name = ConsumerName.Create(SampleData.ConsumerName);
            var customer = new Consumer(CreateConsumerId(), cprNumber, name);

            Assert.Equal(cprNumber, customer.CprNumber);
        }

        [Fact]
        public void Create_UsingCvrNumber_IsSuccessful()
        {
            var cvrNumber = CvrNumber.Create(SampleData.ConsumerVATNumber);
            var name = ConsumerName.Create(SampleData.ConsumerName);
            var customer = new Consumer(CreateConsumerId(), cvrNumber, name);

            Assert.Equal(cvrNumber, customer.CvrNumber);
        }

        [Fact]
        public void Create_IsSuccessful()
        {
            var consumer = new Consumer(CreateConsumerId(), CreateCprNumber(), CreateName());

            ConsumerCreated? @event = consumer.DomainEvents.FirstOrDefault() as ConsumerCreated;

            Assert.NotNull(@event);
            Assert.Equal(consumer.ConsumerId.Value, @event!.ConsumerId);
            Assert.Equal(consumer.CprNumber?.Value, @event.CprNumber);
            Assert.Equal(consumer.CvrNumber?.Value, @event.CvrNumber);
            Assert.Equal(SampleData.ConsumerName, @event.FullName);
        }

        private static ConsumerName CreateName() => ConsumerName.Create(SampleData.ConsumerName);

        private static CprNumber CreateCprNumber() => CprNumber.Create(SampleData.ConsumerSocialSecurityNumber);

        private static ConsumerId CreateConsumerId()
        {
            return new ConsumerId(Guid.NewGuid());
        }
    }
}
