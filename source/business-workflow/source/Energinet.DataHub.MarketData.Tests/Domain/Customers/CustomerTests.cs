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

using Energinet.DataHub.MarketData.Domain.Customers;
using FluentAssertions;
using GreenEnergyHub.TestHelpers.Traits;
using Xunit;

namespace Energinet.DataHub.MarketData.Tests.Domain.Customers
{
    [Trait(TraitNames.Category, TraitValues.UnitTest)]
    public class CustomerTests
    {
        [Fact]
        public void Create_UsingCprNumber_IsSuccessful()
        {
            var cprNumber = CprNumber.Create("2601211234");
            var customer = new Customer(cprNumber);

            Assert.Equal(cprNumber.Value, customer.CustomerId.Value);
        }

        [Fact]
        public void Create_UsingCvrNumber_IsSuccessful()
        {
            var cvrNumber = CvrNumber.Create("10000000");
            var customer = new Customer(cvrNumber);

            Assert.Equal(cvrNumber.Value, customer.CustomerId.Value);
        }
    }
}
