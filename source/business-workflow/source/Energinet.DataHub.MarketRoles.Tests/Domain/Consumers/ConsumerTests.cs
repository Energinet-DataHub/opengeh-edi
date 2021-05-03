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

using Energinet.DataHub.MarketRoles.Domain.Consumers;
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
            var cprNumber = CprNumber.Create("2601211234");
            var customer = new Consumer(CreateConsumerId(), cprNumber);

            Assert.Equal(cprNumber, customer.CprNumber);
        }

        [Fact]
        public void Create_UsingCvrNumber_IsSuccessful()
        {
            var cvrNumber = CvrNumber.Create("10000000");
            var customer = new Consumer(CreateConsumerId(), cvrNumber);

            Assert.Equal(cvrNumber, customer.CvrNumber);
        }

        private ConsumerId CreateConsumerId()
        {
            return new ConsumerId(1);
        }
    }
}
