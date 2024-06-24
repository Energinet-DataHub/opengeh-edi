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
using FluentAssertions.Execution;
using NodaTime;

namespace Energinet.DataHub.EDI.AcceptanceTests.Dsl;

[SuppressMessage(
    "Style",
    "VSTHRD200:Use \"Async\" suffix for async methods",
    Justification = "Dsl classes uses a naming convention based on the business domain")]
public sealed class CalculationCompletedDsl
{
    private readonly Guid _balanceFixingCalculationId;
    private readonly Guid _wholesaleFixingCalculationId;

    private readonly WholesaleDriver _wholesaleDriver;
    private readonly EdiDriver _ediDriver;

    internal CalculationCompletedDsl(AcceptanceTestFixture fixture, EdiDriver ediDriver, WholesaleDriver wholesaleDriver)
    {
        _balanceFixingCalculationId = fixture.BalanceFixingCalculationId;
        _wholesaleFixingCalculationId = fixture.WholesaleFixingCalculationId;
        _wholesaleDriver = wholesaleDriver;
        _ediDriver = ediDriver;
    }

    internal async Task PublishForBalanceFixingCalculation()
    {
        await _ediDriver.EmptyQueueAsync().ConfigureAwait(false);

        var calculationCompletedAt = SystemClock.Instance.GetCurrentInstant();
        await _wholesaleDriver.PublishCalculationCompletedAsync(
            _balanceFixingCalculationId,
            CalculationCompletedV1.Types.CalculationType.BalanceFixing);

        var orchestration = await _ediDriver.WaitForOrchestrationStartedAtAsync(calculationCompletedAt);
        await _ediDriver.WaitForOrchestrationCompletedAtAsync(orchestration.InstanceId);
    }

    internal async Task PublishForWholesaleFixingCalculation()
    {
        await _ediDriver.EmptyQueueAsync().ConfigureAwait(false);

        var calculationCompletedAt = SystemClock.Instance.GetCurrentInstant();
        await _wholesaleDriver.PublishCalculationCompletedAsync(
            _wholesaleFixingCalculationId,
            CalculationCompletedV1.Types.CalculationType.WholesaleFixing);

        var orchestration = await _ediDriver.WaitForOrchestrationStartedAtAsync(calculationCompletedAt);
        await _ediDriver.WaitForOrchestrationCompletedAtAsync(orchestration.InstanceId);
    }

    /// <summary>
    /// Asserts that 5 energy results are available for the actor 5790000392551 as MDR when
    /// the calculation is a balance fixing in the period 1/2/2023 - 3/2/2023
    /// Calculation on d002: https://dev002.datahub3.dk/wholesale/calculations?id=05018715-70cb-4ef4-bfa0-5f75e9f4622e
    /// </summary>
    internal async Task ConfirmEnergyResultsAreAvailable()
    {
        var peekResponses = await _ediDriver.PeekAllMessagesAsync()
            .ConfigureAwait(false);

        using var assertionScope = new AssertionScope();

        peekResponses.Should()
            .HaveCount(5, $"because there should be 5 energy results for actor 5790000392551 as MDR in the calculation {_balanceFixingCalculationId}")
            .And.AllSatisfy(
                r => r.Headers
                    .GetValues("MessageId")
                    .FirstOrDefault()
                    .Should()
                    .NotBeNullOrEmpty(),
                "because all peek responses should contain a MessageId");

        var responseDocuments = await Task.WhenAll(peekResponses.Select(r => r.Content.ReadAsStringAsync()));
        responseDocuments
            .Should()
            .AllSatisfy(
                document => document.Should().Match(
                    "*NotifyAggregatedMeasureData_MarketDocument*",
                    "because the peek responses should contain only NotifyAggregatedMeasureData documents"));

        await _ediDriver.EmptyQueueAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Asserts that 19 wholesale results and 5 energy results are available for the actor 5790000392551 as MDR when
    /// the calculation is a wholesale fixing in the period 1/2/2023 - 28/2/2023
    /// Calculation on d002: https://dev002.datahub3.dk/wholesale/calculations?id=13d57d2d-7e97-410e-9856-85554281770e
    /// </summary>
    internal async Task ConfirmWholesaleResultsAndEnergyResultsAreAvailable()
    {
        var peekResponses = await _ediDriver.PeekAllMessagesAsync()
            .ConfigureAwait(false);

        using var assertionScope = new AssertionScope();
        peekResponses.Should()
            .NotBeEmpty()
            .And.AllSatisfy(
                r => r.Headers
                    .GetValues("MessageId")
                    .FirstOrDefault()
                    .Should()
                    .NotBeNullOrEmpty(),
                "because all peek responses should contain a MessageId");

        var responseDocuments = await Task.WhenAll(peekResponses.Select(r => r.Content.ReadAsStringAsync()));

        var notifyWholesaleServicesDocuments = responseDocuments
            .Where(document => document.Contains("NotifyWholesaleServices_MarketDocument"))
            .ToList();
        notifyWholesaleServicesDocuments.Should().HaveCount(19, $"because there should be 19 wholesale results for actor 5790000392551 as MDR in the calculation {_wholesaleFixingCalculationId}");

        var notifyAggregatedMeasureDataDocuments = responseDocuments
            .Where(document => document.Contains("NotifyAggregatedMeasureData_MarketDocument"))
            .ToList();
        notifyAggregatedMeasureDataDocuments.Should().HaveCount(5, $"because there should be 5 energy results for actor 5790000392551 as MDR in the calculation {_wholesaleFixingCalculationId}");

        await _ediDriver.EmptyQueueAsync().ConfigureAwait(false);
    }
}
