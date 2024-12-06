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
using System.Globalization;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.MeteredDataForMeasurementPoint;
using Energinet.DataHub.EDI.Process.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using NodaTime;
using NodaTime.Text;
using EventId = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.EventId;

namespace Energinet.DataHub.EDI.Process.Application.ProcessInitializationHandlers;

[SuppressMessage(
    "StyleCop.CSharp.ReadabilityRules",
    "SA1118:Parameter should not span multiple lines",
    Justification = "Readability")]
public class InitializeMeteredDataForMeasurementPointHandler(
    IOutgoingMessagesClient outgoingMessagesClient,
    ISerializer serializer,
    ILogger<InitializeMeteredDataForMeasurementPointHandler> logger)
    : IProcessInitializationHandler
{
    private readonly IOutgoingMessagesClient _outgoingMessagesClient = outgoingMessagesClient;
    private readonly ISerializer _serializer = serializer;
    private readonly ILogger<InitializeMeteredDataForMeasurementPointHandler> _logger = logger;

    public bool CanHandle(string processTypeToInitialize)
    {
        ArgumentNullException.ThrowIfNull(processTypeToInitialize);
        return processTypeToInitialize.Equals(nameof(InitializeMeteredDataForMeasurementPointMessageProcessDto), StringComparison.Ordinal);
    }

    public async Task ProcessAsync(byte[] processInitializationData)
    {
        var marketMessage =
            _serializer.Deserialize<InitializeMeteredDataForMeasurementPointMessageProcessDto>(
                System.Text.Encoding.UTF8.GetString(processInitializationData));
        _logger.LogInformation("Received InitializeAggregatedMeasureDataProcess for message {MessageId}", marketMessage.MessageId);

        foreach (var series in marketMessage.Series)
        {
            await _outgoingMessagesClient.EnqueueAndCommitAsync(
                    new MeteredDataForMeasurementPointMessageProcessDto(
                        EventId.From(Guid.NewGuid()),
                        new Actor(ActorNumber.Create("8100000000115"), ActorRole.EnergySupplier),
                        BusinessReason.FromCode(marketMessage.BusinessReason),
                        new MeteredDataForMeasurementPointMessageSeriesDto(
                            TransactionId.From(series.TransactionId),
                            series.MeteringPointLocationId!,
                            series.MeteringPointType!,
                            null,
                            series.ProductNumber!,
                            MeasurementUnit.FromCode(series.ProductUnitType!),
                            InstantPattern.General.Parse(marketMessage.CreatedAt).Value,
                            Resolution.FromCode(series.Resolution!),
                            InstantPattern.Create("yyyy-MM-ddTHH:mm'Z'", CultureInfo.InvariantCulture)
                                .Parse(series.StartDateTime)
                                .Value,
                            series.EndDateTime != null
                                ? InstantPattern.Create("yyyy-MM-ddTHH:mm'Z'", CultureInfo.InvariantCulture)
                                    .Parse(series.EndDateTime)
                                    .Value
                                : throw new ArgumentNullException(),
                            series.EnergyObservations.Select(
                                    o => new EnergyObservationDto(
                                        o.Position != null
                                            ? int.Parse(o.Position)
                                            : throw new ArgumentNullException(nameof(o.Position)),
                                        o.EnergyQuantity != null
                                            ? decimal.Parse(o.EnergyQuantity)
                                            : null,
                                        o.QuantityQuality))
                                .ToList())),
                    CancellationToken.None)
                .ConfigureAwait(false);
        }
    }
}
