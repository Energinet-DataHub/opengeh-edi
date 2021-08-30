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

using Energinet.DataHub.MarketRoles.Domain.EnergySuppliers;
using FluentAssertions;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketRoles.Tests.Domain.GlnNumbers
{
    [UnitTest]
    public class GltNumberTests
    {
        [Theory]
        [InlineData(" ", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        [InlineData("123456", false)]
        [InlineData("123456abcd", false)]
        [InlineData("1234567890", false)]
        [InlineData("12345678901", false)]
        [InlineData("8200000007432", true)]
        [InlineData("5799999911118", true)]
        [InlineData("7495563456235", true)]
        public void Validate_GlnNumber(string glnNumber, bool expectedIsSuccess)
        {
            var actualIsSuccess = GlnNumber.CheckRules(glnNumber).Success;

            actualIsSuccess.Should().Be(expectedIsSuccess);
        }
    }
}
