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

using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketData.Application.ChangeOfSupplier;
using Energinet.DataHub.MarketData.Application.InputValidation;
using Energinet.DataHub.MarketData.Tests.InputValidation.Helpers;
using FluentAssertions;
using GreenEnergyHub.Messaging.MessageTypes.Common;
using NodaTime;
using Xunit;

namespace Energinet.DataHub.MarketData.Tests.InputValidation
{
    [Trait("Category", "Unit")]
    public class InitiateChangeOfSupplierRulesTests
    {
        [Fact]
        public async Task All_input_validations_for_initiate_change_of_supplier_should_fail_for_empty_object()
        {
            var ruleCollectionTester = RuleCollectionTester.Create<InitiateChangeOfSupplierRules, RequestChangeOfSupplier>();
            var initiateChangeOfSupplier = new RequestChangeOfSupplier();

            var result = await ruleCollectionTester.InvokeAsync(initiateChangeOfSupplier).ConfigureAwait(false);

            result.Count.Should().Be(5);
        }

        [Fact]
        public async Task Input_validations_should_not_fail_for_valid_object()
        {
            var ruleCollectionTester = RuleCollectionTester.Create<InitiateChangeOfSupplierRules, RequestChangeOfSupplier>();
            var initiateChangeOfSupplier = new RequestChangeOfSupplier
            {
                MarketEvaluationPoint = new MarketEvaluationPoint { MRid = "571313180400153356" },
                StartDate = Instant.FromUtc(2020, 10, 5, 1, 0),
                BalanceResponsibleParty = new MarketParticipant("8100000000207"),
                EnergySupplier = new MarketParticipant("5790001686758"),
                Consumer = new MarketParticipant("50000000") { Qualifier = "VA" },
            };

            var result = await ruleCollectionTester.InvokeAsync(initiateChangeOfSupplier).ConfigureAwait(false);

            result.Count.Should().Be(0);
        }

        [Theory]
        [ClassData(typeof(MarketEvaluationCollection))]
        public async Task Consumer_should_fail_if_containing_invalid_or_missing_qualifier(MarketParticipant marketParticipant)
        {
            var ruleCollectionTester = RuleCollectionTester.Create<InitiateChangeOfSupplierRules, RequestChangeOfSupplier>();
            var initiateChangeOfSupplier = new RequestChangeOfSupplier
            {
                MarketEvaluationPoint = new MarketEvaluationPoint { MRid = "571313180400153356" },
                StartDate = Instant.FromUtc(2020, 10, 5, 1, 0),
                BalanceResponsibleParty = new MarketParticipant("8100000000207"),
                EnergySupplier = new MarketParticipant("5790001686758"),
                Consumer = marketParticipant,
            };

            var result = await ruleCollectionTester.InvokeAsync(initiateChangeOfSupplier).ConfigureAwait(false);

            result.Count.Should().Be(1);
            result.First().RuleNumber.Should().Be("D17");
        }
    }
}
