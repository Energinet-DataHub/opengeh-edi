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

using System.Text.Json;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Serialization;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.RSM012;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.MarketDocuments;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;
using NodaTime.Extensions;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Migration;

public class TimeSeriesJsonToEbixStreamWriter(
    Serializer serializer,
    JsonSerializerOptions jsonSerializerOptions,
    TimeSeriesJsonToMarketActivityRecordTransformer timeSeriesJsonToMarketActivityRecordTransformer)
    : ITimeSeriesJsonToEbixStreamWriter
{
    private readonly Serializer _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    private readonly JsonSerializerOptions _jsonSerializerOptions = jsonSerializerOptions ?? throw new ArgumentNullException(nameof(jsonSerializerOptions));
    private readonly TimeSeriesJsonToMarketActivityRecordTransformer _timeSeriesJsonToMarketActivityRecordTransformer = timeSeriesJsonToMarketActivityRecordTransformer ?? throw new ArgumentNullException(nameof(timeSeriesJsonToMarketActivityRecordTransformer));
    private readonly MeteredDataForMeteringPointEbixDocumentWriter _writer = new(new MessageRecordParser(serializer));

    public async Task<MarketDocumentStream> WriteStreamAsync(string timeSeriesPayload)
    {
        // Deserialize the JSON into the Root object for processing
        var root = JsonSerializer.Deserialize<Root>(timeSeriesPayload, _jsonSerializerOptions) ?? throw new Exception("Root is null.");

        // Extract the header and time series data
        var series = root.MeteredDataTimeSeriesDH3.TimeSeries;
        var header = root.MeteredDataTimeSeriesDH3.Header;
        var creationTime = header.Creation.ToInstant();

        // Create the outgoing message header using the deserialized header data
        var outgoingMessageHeader = new OutgoingMessageHeader(
            BusinessReason.FromCode(header.EnergyBusinessProcess).Name,
            header.SenderIdentification.Content,
            ActorRole.GridAccessProvider.Code,
            header.RecipientIdentification.Content,
            ActorRole.MeteredDataResponsible.Code,
            header.MessageId,
            null,
            creationTime);

        var meteredDataForMeteringPointMarketActivityRecords = _timeSeriesJsonToMarketActivityRecordTransformer.TransformJsonMessage(creationTime, series);

        // Write the document using the MeteredDataForMeteringPointMarketActivityRecords and the outgoing message header
        var stream = await _writer.WriteAsync(
            outgoingMessageHeader,
            meteredDataForMeteringPointMarketActivityRecords.Select(_serializer.Serialize).ToList(),
            CancellationToken.None).ConfigureAwait(false);

        return stream;
    }
}
