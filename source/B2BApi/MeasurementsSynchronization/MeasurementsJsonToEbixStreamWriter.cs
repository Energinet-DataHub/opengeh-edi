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

using System.Text;
using System.Text.Json;
using System.Xml;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;
using NodaTime.Extensions;

namespace Energinet.DataHub.EDI.B2BApi.MeasurementsSynchronization;

public class MeasurementsJsonToEbixStreamWriter(ISerializer serializer, IEnumerable<IDocumentWriter> documentWriters) : IMeasurementsJsonToEbixStreamWriter
{
    private readonly ISerializer _serializer = serializer;
    private readonly IEnumerable<IDocumentWriter> _documentWriters = documentWriters;

    public async Task<(Stream Document, string Sender)> WriteStreamAsync(BinaryData timeSeriesPayload)
    {
        var json = JsonFromXmlFieldExtractor.ExtractJsonFromXmlCData(Encoding.UTF8.GetString(timeSeriesPayload));

        // Deserialize the JSON into the Root object for processing
        var root = JsonSerializer.Deserialize<Root>(json) ?? throw new Exception("Root is null.");
        json = null;

        // We need to swap the sender and recipient identification in the header
        (root.MeteredDataTimeSeriesDH3.Header.SenderIdentification, root.MeteredDataTimeSeriesDH3.Header.RecipientIdentification) =
            (root.MeteredDataTimeSeriesDH3.Header.RecipientIdentification, root.MeteredDataTimeSeriesDH3.Header.SenderIdentification);

        // Extract the header and time series data
        var series = root.MeteredDataTimeSeriesDH3.TimeSeries;
        var header = root.MeteredDataTimeSeriesDH3.Header;
        var creationTime = header.Creation.ToInstant();

        // Create the outgoing message header using the deserialized header data
        var outgoingMessageHeader = new OutgoingMessageHeader(
            BusinessReason.FromCode(header.EnergyBusinessProcess).Name,
            header.SenderIdentification,
            ActorRole.GridAccessProvider.Code,
            header.RecipientIdentification,
            ActorRole.MeteredDataResponsible.Code,
            header.MessageId,
            null,
            creationTime);

        var meteredDataForMeteringPointMarketActivityRecords = MeasurementsToMarketActivityRecordTransformer.Transform(creationTime, series);

        // Write the document using the MeteredDataForMeteringPointMarketActivityRecords and the outgoing message header
        var marketDocumentStream = await _documentWriters.First(x => x.HandlesType(DocumentType.NotifyValidatedMeasureData) && x.HandlesFormat(DocumentFormat.Ebix)).WriteAsync(
            outgoingMessageHeader,
            meteredDataForMeteringPointMarketActivityRecords.Select(_serializer.Serialize).ToList(),
            CancellationToken.None).ConfigureAwait(false);

        var sender = root.MeteredDataTimeSeriesDH3.Header.SenderIdentification;
        if (sender == null)
            throw new InvalidOperationException("Could not find Sender in the XML.");

        root = null;

        // Load the stream into an XmlDocument for removing the header and formatting.
        var xmlDoc = new XmlDocument();
        xmlDoc.Load(marketDocumentStream.Stream);
        await marketDocumentStream.Stream.DisposeAsync().ConfigureAwait(false);
        var mgr = new XmlNamespaceManager(xmlDoc.NameTable);
        mgr.AddNamespace("b2b", "urn:www:datahub:dk:b2b:v01");
        mgr.AddNamespace("ns0", "un:unece:260:data:EEM-DK_MeteredDataTimeSeries:v3");

        var dataSeriesNode = xmlDoc.SelectSingleNode("//ns0:DK_MeteredDataTimeSeries", mgr);
        if (dataSeriesNode == null)
            throw new InvalidOperationException("Could not find DK_MeteredDataTimeSeries in the XML.");

        var outputStream = new MemoryStream();
        var settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            Indent = true,
            OmitXmlDeclaration = false,
            Async = true,
        };

        using (var writer = XmlWriter.Create(outputStream, settings))
        {
            await writer.WriteStartDocumentAsync().ConfigureAwait(false);
            dataSeriesNode.WriteTo(writer);
            await writer.WriteEndDocumentAsync().ConfigureAwait(false);
        }

        outputStream.Position = 0;
        return (outputStream, sender);
    }
}
