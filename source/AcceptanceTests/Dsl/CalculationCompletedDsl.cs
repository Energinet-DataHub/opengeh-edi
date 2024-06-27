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
using Xunit.Abstractions;

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
    private readonly ITestOutputHelper _logger;
    private readonly EdiDriver _ediDriver;
    private readonly EdiDatabaseDriver _ediDatabaseDriver;

    internal CalculationCompletedDsl(EdiDriver ediDriver, EdiDatabaseDriver ediDatabaseDriver, WholesaleDriver wholesaleDriver, ITestOutputHelper logger, Guid balanceFixingCalculationId, Guid wholesaleFixingCalculationId)
    {
        _balanceFixingCalculationId = balanceFixingCalculationId;
        _wholesaleFixingCalculationId = wholesaleFixingCalculationId;
        _wholesaleDriver = wholesaleDriver;
        _logger = logger;
        _ediDriver = ediDriver;
        _ediDatabaseDriver = ediDatabaseDriver;
    }

    internal async Task PublishForBalanceFixingCalculation()
    {
        await _ediDriver.EmptyQueueAsync();
        await _ediDatabaseDriver.DeleteOutgoingMessagesForCalculationAsync(_balanceFixingCalculationId);

        await StartAndWaitForOrchestrationToComplete(
            CalculationCompletedV1.Types.CalculationType.BalanceFixing,
            _balanceFixingCalculationId);
    }

    internal async Task PublishForWholesaleFixingCalculation()
    {
        await _ediDriver.EmptyQueueAsync();
        await _ediDatabaseDriver.DeleteOutgoingMessagesForCalculationAsync(_wholesaleFixingCalculationId);

        await StartAndWaitForOrchestrationToComplete(
            CalculationCompletedV1.Types.CalculationType.WholesaleFixing,
            _wholesaleFixingCalculationId);
    }

    /// <summary>
    /// Asserts that 5 energy results are available for the actor 5790000392551 as MDR when
    /// the calculation is a balance fixing in the period 1/2/2023 - 3/2/2023 for grid area 804 and 543
    /// Calculation on d002: 92af2ff8-53f9-4242-b066-009c05195007
    /// TODO: Verify that the above is the correct number of energy results for actor 5790000392551 as MDR
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
    /// Asserts that 28 wholesale results and 5 energy results are available for the actor 5790000392551 as MDR when
    /// the calculation is a wholesale fixing in the period 1/2/2023 - 28/2/2023 for grid area 804
    /// Calculation on d002: https://dev002.datahub3.dk/wholesale/calculations?id=13d57d2d-7e97-410e-9856-85554281770e
    /// TODO: Verify that the above is the correct number of energy/wholesale results for actor 5790000392551 as MDR
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
        notifyWholesaleServicesDocuments.Should().HaveCount(28, $"because there should be 28 wholesale results for actor 5790000392551 as MDR in the calculation {_wholesaleFixingCalculationId}");

        var notifyAggregatedMeasureDataDocuments = responseDocuments
            .Where(document => document.Contains("NotifyAggregatedMeasureData_MarketDocument"))
            .ToList();
        notifyAggregatedMeasureDataDocuments.Should().HaveCount(5, $"because there should be 5 energy results for actor 5790000392551 as MDR in the calculation {_wholesaleFixingCalculationId}");

        await _ediDriver.EmptyQueueAsync().ConfigureAwait(false);
    }

    private async Task StartAndWaitForOrchestrationToComplete(
        CalculationCompletedV1.Types.CalculationType calculationType,
        Guid calculationId)
    {
        // Get current instant and subtract 10 second to ensure that the orchestration is started after the instant
        // In some cases the orchestration can be started before a second has passed which meant that the orchestration
        // would not be retrieved by the durable client
        var orchestrationStartedAfter = SystemClock.Instance.GetCurrentInstant().Minus(Duration.FromSeconds(10));

        _logger.WriteLine("Publish calculation completed for calculation with id {0}", _balanceFixingCalculationId);
        await _wholesaleDriver.PublishCalculationCompletedAsync(
            calculationId,
            calculationType);

        _logger.WriteLine("Wait for message orchestration to be started after {0}", orchestrationStartedAfter.ToString());
        var orchestration = await _ediDriver.WaitForOrchestrationStartedAsync(orchestrationStartedAfter);
        orchestration.Input.Value<string>("CalculationId")
            .Should()
            .Be(
                calculationId.ToString(),
                $"because the orchestration should be for the given calculation id {calculationId}");

        _logger.WriteLine("Wait for message orchestration to be completed for instance id {0}", orchestration.InstanceId);
        await _ediDriver.WaitForOrchestrationCompletedAsync(orchestration.InstanceId);

        _logger.WriteLine("Message orchestration completed for instance id {0}, took {1}", orchestration.InstanceId, SystemClock.Instance.GetCurrentInstant() - orchestrationStartedAfter);
    }
}
