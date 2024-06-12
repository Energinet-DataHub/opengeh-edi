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
using Energinet.DataHub.EDI.AcceptanceTests.Drivers;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using FluentAssertions;

namespace Energinet.DataHub.EDI.AcceptanceTests.Dsl;

[SuppressMessage(
    "Style",
    "VSTHRD200:Use \"Async\" suffix for async methods",
    Justification = "Dsl classes uses a naming convention based on the business domain")]
public sealed class CalculationCompletedDsl
{
    // TODO: Use config variables for calculation id's
    private static readonly Guid _balanceFixingCalculationId = Guid.Parse("f0db1f58-e444-4fba-878e-e21b4523c7e1");
    private static readonly Guid _wholesaleFixingCalculationId = Guid.Parse("13d57d2d-7e97-410e-9856-85554281770e");

    private readonly WholesaleDriver _wholesaleDriver;
    private readonly EdiDriver _ediDriver;

    internal CalculationCompletedDsl(EdiDriver ediDriver, WholesaleDriver wholesaleDriver)
    {
        _wholesaleDriver = wholesaleDriver;
        _ediDriver = ediDriver;
    }

    internal async Task PublishForBalanceFixingCalculation()
    {
        await _ediDriver.EmptyQueueAsync().ConfigureAwait(false);

        await _wholesaleDriver.PublishCalculationCompletedAsync(
            _balanceFixingCalculationId,
            CalculationCompletedV1.Types.CalculationType.BalanceFixing);
    }

    internal async Task PublishForWholesaleFixing()
    {
        await _wholesaleDriver.PublishCalculationCompletedAsync(
            _wholesaleFixingCalculationId,
            CalculationCompletedV1.Types.CalculationType.WholesaleFixing);
    }

    internal async Task ConfirmEnergyResultIsAvailable()
    {
        var peekResponse = await _ediDriver.PeekMessageAsync().ConfigureAwait(false);
        var messageId = peekResponse.Headers.GetValues("MessageId").FirstOrDefault();
        var contentString = await peekResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

        messageId.Should().NotBeNull();
        contentString.Should().NotBeNull();
        contentString.Should().Contain("NotifyAggregatedMeasureData_MarketDocument");
    }
}
