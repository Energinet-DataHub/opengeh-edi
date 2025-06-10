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

using System.Globalization;
using System.Text;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;
using NodaTime;

namespace Energinet.DataHub.EDI.IncomingMessages.IntegrationTests.Builders;

public static class RequestMeasurementsBuilder
{
    public static IncomingMarketMessageStream CreateIncomingMessage(
        string messageId,
        DocumentFormat format,
        Actor senderActor,
        BusinessReason businessReason,
        IReadOnlyCollection<(TransactionId TransactionId, Instant PeriodStart, Instant PeriodEnd, MeteringPointId MeteringPointId)> series)
    {
        string content;
        if (format == DocumentFormat.Json)
        {
            content = GetJson(
                messageId,
                senderActor,
                businessReason,
                series);
        }
        else if (format == DocumentFormat.Ebix)
        {
            content = GetEbix(
                messageId,
                senderActor,
                businessReason,
                series);
        }
        else if (format == DocumentFormat.Xml)
        {
            content = GetXml(
                messageId,
                senderActor,
                businessReason,
                series);
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported document format");
        }

        return new IncomingMarketMessageStream(new MemoryStream(Encoding.UTF8.GetBytes(content)));
    }

    private static string GetEbix(
        string messageId,
        Actor sender,
        BusinessReason businessReason,
        IReadOnlyCollection<(TransactionId TransactionId, Instant PeriodStart, Instant PeriodEnd, MeteringPointId
            MeteringPointId)> series) =>
        $$"""
          <?xml version="1.0" encoding="UTF-8"?>
          <ns0:DK_RequestMeteredDataValidated xmlns:ns0="un:unece:260:data:EEM-DK_RequestMeteredDataValidated:v3">
              <ns0:HeaderEnergyDocument>
                  <ns0:Identification>{{messageId}}</ns0:Identification>
                  <ns0:DocumentType listAgencyIdentifier="260">E73</ns0:DocumentType>
                  <ns0:Creation>2022-12-17T09:30:47Z</ns0:Creation>
                  <ns0:SenderEnergyParty>
                      <ns0:Identification schemeAgencyIdentifier="9">{{sender.ActorNumber.Value}}</ns0:Identification>
                  </ns0:SenderEnergyParty>
                  <ns0:RecipientEnergyParty>
                      <ns0:Identification schemeAgencyIdentifier="9">5790001330552</ns0:Identification>
                  </ns0:RecipientEnergyParty>
              </ns0:HeaderEnergyDocument>
              <ns0:ProcessEnergyContext>
                  <ns0:EnergyBusinessProcess listAgencyIdentifier="260">{{businessReason.Code}}</ns0:EnergyBusinessProcess>
                  <ns0:EnergyBusinessProcessRole listAgencyIdentifier="260">DDQ</ns0:EnergyBusinessProcessRole>
                  <ns0:EnergyIndustryClassification listAgencyIdentifier="6">23</ns0:EnergyIndustryClassification>
              </ns0:ProcessEnergyContext>
              {{string.Join(",\n", series.Select(s =>
                  $$"""
                    <ns0:PayloadMeasuredDataRequest>
                        <ns0:Identification>{{s.TransactionId.Value}}</ns0:Identification>
                        <ns0:ObservationTimeSeriesPeriod>
                            <ns0:Start>{{s.PeriodStart}}</ns0:Start>
                            <ns0:End>{{s.PeriodEnd}}</ns0:End>
                        </ns0:ObservationTimeSeriesPeriod>
                        <ns0:MeteringPointDomainLocation>
                            <ns0:Identification schemeAgencyIdentifier="9">{{s.MeteringPointId.Value}}</ns0:Identification>
                        </ns0:MeteringPointDomainLocation>
                    </ns0:PayloadMeasuredDataRequest>
                    """))}}
          </ns0:DK_RequestMeteredDataValidated>
          """;

    private static string GetJson(
          string messageId,
          Actor sender,
          BusinessReason businessReason,
          IReadOnlyCollection<(TransactionId TransactionId, Instant PeriodStart, Instant PeriodEnd, MeteringPointId MeteringPointId)> series) =>
      $$"""
      {
        "RequestValidatedMeasureData_MarketDocument": {
          "mRID": "{{messageId}}",
          "businessSector.type": {
            "value": "23"
          },
          "createdDateTime": "2022-12-17T09:30:47Z",
          "process.processType": {
            "value": "{{businessReason.Code}}"
          },
          "receiver_MarketParticipant.mRID": {
            "codingScheme": "A10",
            "value": "5790001330552"
          },
          "receiver_MarketParticipant.marketRole.type": {
            "value": "DGL"
          },
          "sender_MarketParticipant.mRID": {
            "codingScheme": "A10",
            "value": "{{sender.ActorNumber.Value}}"
          },
          "sender_MarketParticipant.marketRole.type": {
            "value": "{{sender.ActorRole.Code}}"
          },
          "type": {
            "value": "E73"
          },
          "Series": [
          {{string.Join(",\n", series.Select(s =>
              $$"""
                {
                  "mRID": "{{s.TransactionId.Value}}",
                  "end_DateAndOrTime.dateTime": "{{s.PeriodEnd}}",
                  "marketEvaluationPoint.mRID": {
                    "codingScheme": "A10",
                    "value": "{{s.MeteringPointId.Value}}"
                  },
                  "start_DateAndOrTime.dateTime": "{{s.PeriodStart}}"    
                }  
                """))}}
          ]
        }
      }
      """;

    private static string GetXml(
        string messageId,
        Actor sender,
        BusinessReason businessReason,
        IReadOnlyCollection<(TransactionId TransactionId, Instant PeriodStart, Instant PeriodEnd, MeteringPointId
            MeteringPointId)> series) =>
        $$"""
          <cim:RequestValidatedMeasureData_MarketDocument xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:cim="urn:ediel.org:measure:requestvalidatedmeasuredata:0:1" xsi:schemaLocation="urn:ediel.org:measure:requestvalidatedmeasuredata:0:1 urn-ediel-org-measure-requestvalidatedmeasuredata-0-1.xsd">
          	<cim:mRID>{{messageId}}</cim:mRID>
          	<cim:type>E73</cim:type>
          	<cim:process.processType>{{businessReason.Code}}</cim:process.processType>
          	<cim:businessSector.type>23</cim:businessSector.type>
          	<cim:sender_MarketParticipant.mRID codingScheme="A10">{{sender.ActorNumber.Value}}</cim:sender_MarketParticipant.mRID>
          	<cim:sender_MarketParticipant.marketRole.type>{{sender.ActorRole.Code}}</cim:sender_MarketParticipant.marketRole.type>
          	<cim:receiver_MarketParticipant.mRID codingScheme="A10">5790001330552</cim:receiver_MarketParticipant.mRID>
          	<cim:receiver_MarketParticipant.marketRole.type>DGL</cim:receiver_MarketParticipant.marketRole.type>
          	<cim:createdDateTime>2001-12-17T09:30:47Z</cim:createdDateTime>
          	{{string.Join("\n", series.Select(s =>
                  $$"""
                    <cim:Series>
                    	<cim:mRID>{{s.TransactionId.Value}}</cim:mRID>
                    	<cim:start_DateAndOrTime.dateTime>{{s.PeriodStart}}</cim:start_DateAndOrTime.dateTime>
                    	<cim:end_DateAndOrTime.dateTime>{{s.PeriodEnd}}</cim:end_DateAndOrTime.dateTime>
                    	<cim:marketEvaluationPoint.mRID codingScheme="A10">{{s.MeteringPointId.Value}}</cim:marketEvaluationPoint.mRID>
                    </cim:Series>
                    """))}}
          </cim:RequestValidatedMeasureData_MarketDocument>
          """;
}
