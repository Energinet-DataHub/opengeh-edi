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
        string? schema = null)
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
                schema ?? "un:unece:260:data:EEM-DK_MeteredDataTimeSeries:v3");
        }
        else if (format == DocumentFormat.Xml)
        {
            content = GetXml(
                senderActorNumber,
                series,
                messageType,
                processType,
                businessType,
                messageId,
                senderRole,
                receiverNumber,
                schema ?? "urn:ediel.org:measure:notifyvalidatedmeasuredata:0:1");
        }
        else if (format == DocumentFormat.Json)
        {
            content = GetJson(
                senderActorNumber,
                series,
                messageType,
                processType,
                businessType,
                messageId,
                senderRole,
                receiverNumber);
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
        {EnergyObservationEbixBuilder(GetEnergyObservations())}
    </ns0:PayloadEnergyTimeSeries>
    "))}
</ns0:DK_MeteredDataTimeSeries>");
        return doc.OuterXml;
    }

    private static string GetXml(
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
<cim:NotifyValidatedMeasureData_MarketDocument xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:cim=""{schema}"" xsi:schemaLocation=""{schema} urn-ediel-org-measure-notifyvalidatedmeasuredata-0-1.xsd"">
    <cim:mRID>{messageId}</cim:mRID>
    <cim:type>{messageType}</cim:type>
    <cim:process.processType>{processType}</cim:process.processType>
    <cim:businessSector.type>{businessType}</cim:businessSector.type>
    <cim:sender_MarketParticipant.mRID codingScheme=""A10"">{senderActorNumber.Value}</cim:sender_MarketParticipant.mRID>
    <cim:sender_MarketParticipant.marketRole.type>{senderRole}</cim:sender_MarketParticipant.marketRole.type>
    <cim:receiver_MarketParticipant.mRID codingScheme=""A10"">{receiverNumber}</cim:receiver_MarketParticipant.mRID>
    <cim:receiver_MarketParticipant.marketRole.type>DGL</cim:receiver_MarketParticipant.marketRole.type>
    <cim:createdDateTime>2022-12-17T09:30:47Z</cim:createdDateTime>
    
    {string.Join("\n", series.Select(s => $@"
        <cim:Series>
            <cim:mRID>{s.TransactionId}</cim:mRID>
            <cim:originalTransactionIDReference_Series.mRID>C1875000</cim:originalTransactionIDReference_Series.mRID>
            <!--  Kun ved svar på anmodning (RSM-015)  -->
            <cim:marketEvaluationPoint.mRID codingScheme=""A10"">579999993331812345</cim:marketEvaluationPoint.mRID>
            <!--    -->
            <cim:marketEvaluationPoint.type>E17</cim:marketEvaluationPoint.type>
            <!--   -->
            <cim:registration_DateAndOrTime.dateTime>2022-12-17T07:30:00Z</cim:registration_DateAndOrTime.dateTime>
            <!--    -->
            <cim:product>8716867000030</cim:product>
            <cim:quantity_Measure_Unit.name>KWH</cim:quantity_Measure_Unit.name>
            <cim:Period>
                <cim:resolution>{s.Resolution.Code}</cim:resolution>
                <cim:timeInterval>
                    <cim:start>{s.PeriodStart.ToString("yyyy-MM-ddTHH:mm'Z'", null)}</cim:start>
                    <cim:end>{s.PeriodEnd.ToString("yyyy-MM-ddTHH:mm'Z'", null)}</cim:end>
                </cim:timeInterval>

            {EnergyObservationXmlBuilder(GetEnergyObservations())}
        </cim:Period>
    </cim:Series>
    "))}
</cim:NotifyValidatedMeasureData_MarketDocument>");
        return doc.OuterXml;
    }

    private static string GetJson(
        ActorNumber senderActorNumber,
        IReadOnlyCollection<(string TransactionId, Instant PeriodStart, Instant PeriodEnd, Resolution Resolution)>
            series,
        string messageType,
        string processType,
        string businessType,
        string messageId,
        string senderRole,
        string receiverNumber) =>
        $$"""
          {
            "NotifyValidatedMeasureData_MarketDocument": {
              "mRID": "{{messageId}}",
              "businessSector.type": {
                "value": "{{businessType}}"
              },
              "createdDateTime": "2022-12-17T09:30:47Z",
              "process.processType": {
          	     "value": "{{processType}}"
              },
              "receiver_MarketParticipant.mRID": {
          	   "codingScheme": "A10",
          	     "value": "{{receiverNumber}}"
              },
              "receiver_MarketParticipant.marketRole.type": {
          	     "value": "DGL"
              },
              "sender_MarketParticipant.mRID": {
          	     "codingScheme": "A10",
          	     "value": "{{senderActorNumber.Value}}"
              },
              "sender_MarketParticipant.marketRole.type": {
          	     "value": "{{senderRole}}"
              },
              "type": {
          	     "value": "{{messageType}}"
              },
              "Series": [
                {{string.Join(",\n", series.Select(s =>
                    $$"""
                      {
                        "mRID": "{{s.TransactionId}}",
                        "marketEvaluationPoint.mRID": {
                          "codingScheme": "A10",
                          "value": "579999993331812345"
                        },
                        "marketEvaluationPoint.type": {
                          "value": "E17"
                        },
                        "originalTransactionIDReference_Series.mRID": "C1875000",
                        "product": "8716867000030",
                        "quantity_Measure_Unit.name": {
                          "value": "KWH"
                        },
                        "registration_DateAndOrTime.dateTime": "2022-12-17T07:30:00Z",
                        "Period": {
                          "resolution": "{{s.Resolution.Code}}",
                          "timeInterval": {
                            "start": {
                              "value": "{{s.PeriodStart.ToString("yyyy-MM-ddTHH:mm'Z'", null)}}"
                            },
                            "end": {
                              "value": "{{s.PeriodEnd.ToString("yyyy-MM-ddTHH:mm'Z'", null)}}"
                            }
                          },
                          "Point": [
                            {{EnergyObservationJsonBuilder(GetEnergyObservations())}}
                          ]
                        }
                      }
                      """))}}
              ]
            }
          }
          """;

    private static IReadOnlyCollection<(int Position, string? Quality, decimal? Quantity)> GetEnergyObservations() =>
    [
        (1, null, null),
        (2, "A03", null),
        (3, null, 123.456m),
        (4, "A03", 654.321m),
    ];

    private static string EnergyObservationJsonBuilder(
        IReadOnlyCollection<(int Position, string? Quality, decimal? Quantity)> observations)
    {
        return string.Join(
            ",\n",
            observations.Select(
                e =>
                {
                    var builder = new StringBuilder();
                    builder.Append($"{{ \"position\": {{ \"value\": {e.Position} }}");

                    if (e.Quality != null)
                    {
                        builder.Append($", \"quality\": {{ \"value\": \"{e.Quality}\" }}");
                    }

                    if (e.Quantity.HasValue)
                    {
                        builder.Append($", \"quantity\": {e.Quantity.Value.ToString(CultureInfo.InvariantCulture)}");
                    }

                    builder.Append(" }");
                    return builder.ToString();
                }));
    }

    private static string EnergyObservationXmlBuilder(
        IReadOnlyCollection<(int Position, string? Quality, decimal? Quantity)> observations)
    {
        return string.Join(
            "\n",
            observations.Select(
                e =>
                {
                    var builder = new StringBuilder();
                    builder.AppendLine("<cim:Point>");
                    builder.AppendLine($"<cim:position>{e.Position}</cim:position>");

                    if (e.Quantity.HasValue)
                    {
                        builder.AppendLine(
                            $"<cim:quantity>{e.Quantity.Value.ToString(CultureInfo.InvariantCulture)}</cim:quantity>");
                    }

                    if (e.Quality != null)
                    {
                        builder.AppendLine($"<cim:quality>{e.Quality}</cim:quality>");
                    }

                    builder.AppendLine("</cim:Point>");

                    return builder.ToString();
                }));
    }

    private static string EnergyObservationEbixBuilder(
        IReadOnlyCollection<(int Position, string? Quality, decimal? Quantity)> observations)
    {
        return string.Join(
            "\n",
            observations.Select(
                e =>
                {
                    var builder = new StringBuilder();
                    builder.AppendLine("<ns0:IntervalEnergyObservation>");
                    builder.AppendLine($"<ns0:Position>{e.Position}</ns0:Position>");

                    if (e.Quantity.HasValue)
                    {
                        builder.AppendLine(
                            $"<ns0:EnergyQuantity>{e.Quantity.Value.ToString(CultureInfo.InvariantCulture)}</ns0:EnergyQuantity>");
                    }
                    else
                    {
                        builder.AppendLine("<ns0:QuantityMissing>true</ns0:QuantityMissing>");
                    }

                    if (e.Quality != null)
                    {
                        builder.AppendLine(
                            "<ns0:QuantityQuality listAgencyIdentifier=\"260\">E01</ns0:QuantityQuality>");
                    }

                    builder.AppendLine("</ns0:IntervalEnergyObservation>");

                    return builder.ToString();
                }));
    }
}
