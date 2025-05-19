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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.SubsystemTests.Drivers;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_021.Shared.V1.Model;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_045.MissingMeasurementsLogCalculation.V1.Model;
using NodaTime;
using NodaTime.Text;
using MeasurementUnit = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects.MeasurementUnit;
using MeteringPointType = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects.MeteringPointType;
using Quality = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects.Quality;
using Resolution = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects.Resolution;

namespace Energinet.DataHub.EDI.SubsystemTests.Dsl;

[SuppressMessage(
    "Style",
    "VSTHRD200:Use \"Async\" suffix for async methods",
    Justification = "Dsl classes uses a naming convention based on the business domain")]
internal class EnqueueActorMessagesHttpDsl
{
    private readonly EdiDriver _ediDriver;
    private readonly SubsystemDriver _subsystemDriver;
    private readonly Instant _rsm018Date = Instant.FromUtc(2025, 05, 11, 22, 00);
    private readonly string _rsm018MeteringPointId = "1111111111111111";

    public EnqueueActorMessagesHttpDsl(
        EdiDriver ediDriver,
        SubsystemDriver subsystemDriver)
    {
        _ediDriver = ediDriver;
        _subsystemDriver = subsystemDriver;
    }

    internal async Task EnqueueElectricalHeatingMessage(Actor receiver, string meteringPointId)
    {
        await _ediDriver.EmptyQueueAsync(MessageCategory.MeasureData).ConfigureAwait(false);

        var processManagerReceiver = new EnqueueCalculatedMeasurementsHttpV1.Actor(
            receiver.ActorNumber.ToProcessManagerActorNumber(),
            receiver.ActorRole.ToProcessManagerActorRole());

        var start = Instant.FromUtc(2025, 01, 31, 23, 00, 00);

        var receiversWithMeasurements = new EnqueueCalculatedMeasurementsHttpV1.ReceiversWithMeasurements(
            Receivers: [processManagerReceiver],
            RegistrationDateTime: start.ToDateTimeOffset(),
            StartDateTime: start.ToDateTimeOffset(),
            EndDateTime: start.Plus(Duration.FromHours(1)).ToDateTimeOffset(),
            Measurements:
            [
                new EnqueueCalculatedMeasurementsHttpV1.Measurement(
                    Position: 1,
                    EnergyQuantity: 019293m,
                    QuantityQuality: Quality.AsProvided),
            ],
            GridAreaCode: "804");

        var message = new EnqueueCalculatedMeasurementsHttpV1(
            OrchestrationInstanceId: Guid.NewGuid(),
            TransactionId: Guid.NewGuid(),
            MeteringPointId: meteringPointId,
            MeteringPointType: MeteringPointType.ElectricalHeating,
            Resolution: Resolution.Hourly,
            MeasureUnit: MeasurementUnit.KilowattHour,
            Data: [receiversWithMeasurements]);

        await _subsystemDriver.EnqueueActorMessagesViaHttpAsync(message);
    }

    internal async Task ConfirmRsm012MessageIsAvailable(string meteringPointId)
    {
        var timeout = TimeSpan.FromMinutes(2); // Timeout must be above 1 minute, since bundling "duration" is set to 1 minute on dev/test.

        var (peekResponse, dequeueResponse) = await _ediDriver.PeekMessageAsync(
            documentFormat: DocumentFormat.Json,
            messageCategory: MessageCategory.MeasureData,
            timeout: timeout);

        var contentString = await peekResponse.Content.ReadAsStringAsync();

        AssertDocumentTypeIsRsm012(contentString);
        AssertMeteringPointIdForRsm012(contentString, meteringPointId);
    }

    internal async Task EnqueueMissingMeasurementsLogMessage(Actor receiver)
    {
        await _ediDriver.EmptyQueueAsync(MessageCategory.Aggregations).ConfigureAwait(false);

        var message = new EnqueueMissingMeasurementsLogHttpV1(
            OrchestrationInstanceId: Guid.NewGuid(),
            Data:
            [
                new EnqueueMissingMeasurementsLogHttpV1.DateWithMeteringPointId(
                    IdempotencyKey: Guid.NewGuid(),
                    GridAccessProvider: receiver.ActorNumber.ToProcessManagerActorNumber(),
                    GridArea: "001",
                    Date: _rsm018Date.ToDateTimeOffset(),
                    MeteringPointId: _rsm018MeteringPointId),
            ]);

        await _subsystemDriver.EnqueueActorMessagesViaHttpAsync(message);
    }

    internal async Task ConfirmRsm018MessageIsAvailable()
    {
        var timeout = TimeSpan.FromMinutes(2); // Timeout must be above 1 minute, since bundling "duration" is set to 1 minute on dev/test.

        var (peekResponse, dequeueResponse) = await _ediDriver.PeekMessageAsync(
            documentFormat: DocumentFormat.Json,
            messageCategory: MessageCategory.MeasureData,
            timeout: timeout);

        var contentString = await peekResponse.Content.ReadAsStringAsync();

        AssertDocumentTypeIsRsm018(contentString);
        AssertDateForRsm018(contentString, _rsm018Date);
        AssertMeteringPointIdForRsm018(contentString, _rsm018MeteringPointId);
    }

    private void AssertDocumentTypeIsRsm012(string content)
    {
        Assert.Contains(
            "NotifyValidatedMeasureData_MarketDocument",
            content);
    }

    private void AssertMeteringPointIdForRsm012(string content, string meteringPointId)
    {
        var expectedMeteringPointIdFormatted = string.Empty
                                              + "        \"marketEvaluationPoint.mRID\": {\r\n"
                                              + "          \"codingScheme\": \"A10\",\r\n"
                                              + $"          \"value\": \"{meteringPointId}\"\r\n"
                                              + "        },";
        Assert.Contains(
            expectedMeteringPointIdFormatted,
            content);
    }

    private void AssertDocumentTypeIsRsm018(string content)
    {
        Assert.Contains(
            "ReminderOfMissingMeasureData_MarketDocument",
            content);
    }

    private void AssertDateForRsm018(string content, Instant date)
    {
        Assert.Contains(
            $"\"request_DateAndOrTime.dateTime\": \"{InstantPattern.General.Format(date)}\"",
            content);
    }

    private void AssertMeteringPointIdForRsm018(string content, string meteringPointId)
    {
        var expectedMeteringPointIdFormatted = string.Empty
           + "        \"MarketEvaluationPoint.mRID\": {\r\n"
           + "          \"codingScheme\": \"A10\",\r\n"
           + $"          \"value\": \"{meteringPointId}\"\r\n"
           + "        },";

        Assert.Contains(
            expectedMeteringPointIdFormatted,
            content);
    }
}
