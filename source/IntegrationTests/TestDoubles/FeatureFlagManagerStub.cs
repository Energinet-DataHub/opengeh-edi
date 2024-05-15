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

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using BuildingBlocks.Application.FeatureFlag;

namespace Energinet.DataHub.EDI.IntegrationTests.TestDoubles;

/// <summary>
/// A FeatureFlagManager used to set default values and which allows overriding feature flags during tests
/// </summary>
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Tests")]
public class FeatureFlagManagerStub : IFeatureFlagManager
{
    private bool _useAmountPerChargeResultProduced = true;
    private bool _useMonthlyAmountPerChargeResultProducedAsync = true;

    public void EnableAmountPerChargeResultProduced(bool enable) => _useAmountPerChargeResultProduced = enable;

    public void EnableMonthlyAmountPerChargeResultProduced(bool enable) => _useMonthlyAmountPerChargeResultProducedAsync = enable;

    public Task<bool> UseExampleFeatureFlagAsync() => Task.FromResult(true);

    public Task<bool> UseMonthlyAmountPerChargeResultProducedAsync() => Task.FromResult(_useMonthlyAmountPerChargeResultProducedAsync);

    public Task<bool> UseAmountPerChargeResultProducedAsync() => Task.FromResult(_useAmountPerChargeResultProduced);

    public Task<bool> UseRequestWholesaleSettlementReceiverAsync() => Task.FromResult(true);

    public Task<bool> UseMessageDelegationAsync() => Task.FromResult(true);

    public Task<bool> UsePeekMessagesAsync() => Task.FromResult(true);

    public Task<bool> UseRequestMessagesAsync() => Task.FromResult(true);

    public Task<bool> UseEnergyResultProducedAsync() => Task.FromResult(true);

    public Task<bool> UseCalculationResultsCompletedEventAsync() => Task.FromResult(false);
}
