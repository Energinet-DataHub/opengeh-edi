﻿// Copyright 2020 Energinet DataHub A/S
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

using System.Collections.ObjectModel;
using System.Text;
using System.Xml;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;
using NodaTime;

namespace Energinet.DataHub.EDI.IncomingMessages.IntegrationTests.Builders;

public static class MeteredDataForMeasurementPointBuilder
{
    public static IncomingMarketMessageStream CreateIncomingMessage(
        DocumentFormat format,
        ActorNumber senderActorNumber,
        IReadOnlyCollection<(string TransactionId, Instant PeriodStart, Instant PeriodEnd, Resolution Resolution)> series,
        string messageType = "E66",
        string processType = "E23",
        string businessType = "23",
        string messageId = "111131835",
        string senderRole = "MDR",
        string receiverNumber = "5790001330552",
        string schema = "un:unece:260:data:EEM-DK_MeteredDataTimeSeries:v3")
    {
        string content;
        if (format == DocumentFormat.Ebix)
        {
            content = GetEbix(
                senderActorNumber,
                series,
                messageType,
                processType,
                businessType,
                messageId,
                senderRole,
                receiverNumber,
                schema);
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported document format");
        }

        return new IncomingMarketMessageStream(new MemoryStream(Encoding.UTF8.GetBytes(content)));
    }

    private static string GetEbix(
        ActorNumber senderActorNumber,
        IReadOnlyCollection<(string TransactionId, Instant PeriodStart, Instant PeriodEnd, Resolution Resolution)> series,
        string messageType,
        string processType,
        string businessType,
        string messageId,
        string senderRole,
        string receiverNumber,
        string schema)
    {
        var doc = new XmlDocument();
        doc.LoadXml($@"
<ns0:DK_MeteredDataTimeSeries xmlns:ns0=""{schema}"">
    <ns0:HeaderEnergyDocument>
        <ns0:Identification>{messageId}</ns0:Identification>
        <ns0:DocumentType listAgencyIdentifier=""260"">{messageType}</ns0:DocumentType>
        <ns0:Creation>2024-07-30T07:30:54Z</ns0:Creation>
        <ns0:SenderEnergyParty>
            <ns0:Identification schemeAgencyIdentifier=""9"">{senderActorNumber.Value}</ns0:Identification>
        </ns0:SenderEnergyParty>
        <ns0:RecipientEnergyParty>
            <ns0:Identification schemeAgencyIdentifier=""9"">{receiverNumber}</ns0:Identification>
        </ns0:RecipientEnergyParty>
    </ns0:HeaderEnergyDocument>
    <ns0:ProcessEnergyContext>
        <ns0:EnergyBusinessProcess listAgencyIdentifier=""260"">{processType}</ns0:EnergyBusinessProcess>
        <ns0:EnergyBusinessProcessRole listAgencyIdentifier=""260"">{senderRole}</ns0:EnergyBusinessProcessRole>
        <ns0:EnergyIndustryClassification listAgencyIdentifier=""6"">{businessType}</ns0:EnergyIndustryClassification>
    </ns0:ProcessEnergyContext>
    {string.Join("\n", series.Select(s => $@"
        <ns0:PayloadEnergyTimeSeries>
            <ns0:Identification>{s.TransactionId}</ns0:Identification>
            <ns0:Function listAgencyIdentifier=""6"">9</ns0:Function>
            <ns0:ObservationTimeSeriesPeriod>
                <ns0:ResolutionDuration>{s.Resolution}</ns0:ResolutionDuration>
                <ns0:Start>{s.PeriodStart}</ns0:Start>
                <ns0:End>{s.PeriodEnd}</ns0:End>
            </ns0:ObservationTimeSeriesPeriod>
            <ns0:IncludedProductCharacteristic>
                <ns0:Identification listAgencyIdentifier=""9"">8716867000030</ns0:Identification>
                <ns0:UnitType listAgencyIdentifier=""260"">KWH</ns0:UnitType>
            </ns0:IncludedProductCharacteristic>
            <ns0:DetailMeasurementMeteringPointCharacteristic>
                <ns0:TypeOfMeteringPoint listAgencyIdentifier=""260"">E18</ns0:TypeOfMeteringPoint>
            </ns0:DetailMeasurementMeteringPointCharacteristic>
            <ns0:MeteringPointDomainLocation>
                <ns0:Identification schemeAgencyIdentifier=""9"">571313000000002000</ns0:Identification>
            </ns0:MeteringPointDomainLocation>
        {string.Join("\n", GetEnergyObservations(s.Resolution).Select(e => $@"
            <ns0:IntervalEnergyObservation>
                <ns0:Position>{e.Position}</ns0:Position>
                <ns0:EnergyQuantity>{e.Quantity}</ns0:EnergyQuantity>
                <ns0:QuantityQuality listAgencyIdentifier=""260"">E01</ns0:QuantityQuality>
            </ns0:IntervalEnergyObservation>
        "))}
    </ns0:PayloadEnergyTimeSeries>
    "))}
</ns0:DK_MeteredDataTimeSeries>");
        return doc.OuterXml;
    }

    private static ReadOnlyCollection<(int Position, int Quantity)> GetEnergyObservations(Resolution resolution)
    {
        var observations = new List<(int Position, int Quantity)>();
        var intervalsPerDay = resolution == Resolution.QuarterHourly ? 96 : 24;

        for (var i = 1; i <= intervalsPerDay; i++)
        {
            observations.Add((i, 1000 + i));
        }

        return observations.AsReadOnly();
    }
}