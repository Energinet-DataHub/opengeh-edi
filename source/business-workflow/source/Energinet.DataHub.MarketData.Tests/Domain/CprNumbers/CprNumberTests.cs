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
using Xunit;

namespace Energinet.DataHub.MarketData.Tests.Domain.CprNumbers
{
    [Trait("Category", "Unit")]
    public class CprNumberTests
    {
        [Theory]
        [InlineData("123456", false)]
        [InlineData("123456abcd", false)]
        [InlineData("1234567890", false)]
        [InlineData("12345678901", false)]
        [InlineData("2601211234", true)]
        public void Validate_Cpr_Number(string cprNumber, bool expectedIsSuccess)
        {
            var actualIsSuccess = CprNumber.CheckRules(cprNumber).Success;

            actualIsSuccess.Should().Be(expectedIsSuccess);
        }
    }
}
