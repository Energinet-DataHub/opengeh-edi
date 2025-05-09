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
using NodaTime;
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
            Receivers: new[] { processManagerReceiver },
            RegistrationDateTime: start.ToDateTimeOffset(),
            StartDateTime: start.ToDateTimeOffset(),
            EndDateTime: start.Plus(Duration.FromHours(1)).ToDateTimeOffset(),
            Measurements: new[]
            {
                new EnqueueCalculatedMeasurementsHttpV1.Measurement(
                    Position: 1,
                    EnergyQuantity: 019293m,
                    QuantityQuality: Quality.AsProvided),
            },
            GridAreaCode: "804");

        var message = new EnqueueCalculatedMeasurementsHttpV1(
            OrchestrationInstanceId: Guid.NewGuid(),
            TransactionId: Guid.NewGuid(),
            MeteringPointId: meteringPointId,
            MeteringPointType: MeteringPointType.ElectricalHeating,
            Resolution: Resolution.Hourly,
            MeasureUnit: MeasurementUnit.KilowattHour,
            Data: new[] { receiversWithMeasurements });

        await _subsystemDriver.EnqueueActorMessagesViaHttpAsync(message);
    }

    internal async Task ConfirmMessageIsAvailable(string meteringPointId)
    {
        var timeout = TimeSpan.FromMinutes(2); // Timeout must be above 1 minute, since bundling "duration" is set to 1 minute on dev/test.

        var (peekResponse, dequeueResponse) = await _ediDriver.PeekMessageAsync(
            documentFormat: DocumentFormat.Json,
            messageCategory: MessageCategory.MeasureData,
            timeout: timeout);

        var contentString = await peekResponse.Content.ReadAsStringAsync();

        AssertDocumentType(contentString);
        AssertMeteringPointId(contentString, meteringPointId);
    }

    private void AssertDocumentType(string content)
    {
        Assert.Contains(
            "NotifyValidatedMeasureData_MarketDocument",
            content);
    }

    private void AssertMeteringPointId(string content, string meteringPointId)
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
}
