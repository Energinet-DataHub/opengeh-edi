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
                        EventId.From(marketMessage.MessageId),
                        new Actor(series.RequestedByActor.ActorNumber, series.RequestedByActor.ActorRole),
                        BusinessReason.FromCode(marketMessage.BusinessReason),
                        marketMessage.Series.Select(
                                s => new MeteredDataForMeasurementPointMessageSeriesDto(
                                    s.TransactionId,
                                    Resolution.FromCode(s.Resolution!),
                                    InstantPattern.Create("yyyy-MM-ddTHH:mm'Z'", CultureInfo.InvariantCulture)
                                        .Parse(s.StartDateTime)
                                        .Value,
                                    s.EndDateTime != null
                                        ? InstantPattern.Create("yyyy-MM-ddTHH:mm'Z'", CultureInfo.InvariantCulture)
                                            .Parse(s.EndDateTime)
                                            .Value
                                        : null,
                                    s.ProductNumber,
                                    s.ProductUnitType,
                                    s.MeteringPointType,
                                    s.MeteringPointLocationId,
                                    s.DelegatedGridAreaCodes,
                                    s.EnergyObservations.Select(
                                            o => new EnergyObservationDto(
                                                o.Position,
                                                o.EnergyQuantity,
                                                o.QuantityQuality))
                                        .ToList()))
                            .ToList()),
                    CancellationToken.None)
                .ConfigureAwait(false);
        }
    }
}
