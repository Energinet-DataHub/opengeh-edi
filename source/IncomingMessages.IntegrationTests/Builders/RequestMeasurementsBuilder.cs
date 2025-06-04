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
        IReadOnlyCollection<(TransactionId TransactionId, Instant PeriodStart, Instant PeriodEnd, MeteringPointId MeteringPointId)> series)
    {
        string content;
        if (format == DocumentFormat.Json)
        {
            content = GetJson(
                messageId,
                senderActor,
                series);
        }
        else if (format == DocumentFormat.Xml)
        {
            content = GetXml(
                messageId,
                senderActor,
                series);
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported document format");
        }

        return new IncomingMarketMessageStream(new MemoryStream(Encoding.UTF8.GetBytes(content)));
    }

    private static string GetJson(
        string messageId,
        Actor sender,
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
            "value": "E23"
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
        IReadOnlyCollection<(TransactionId TransactionId, Instant PeriodStart, Instant PeriodEnd, MeteringPointId
            MeteringPointId)> series) =>
        $$"""
          <cim:RequestValidatedMeasureData_MarketDocument xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:cim="urn:ediel.org:measure:requestvalidatedmeasuredata:0:1" xsi:schemaLocation="urn:ediel.org:measure:requestvalidatedmeasuredata:0:1 urn-ediel-org-measure-requestvalidatedmeasuredata-0-1.xsd">
          	<cim:mRID>{{messageId}}</cim:mRID>
          	<cim:type>E73</cim:type>
          	<cim:process.processType>E23</cim:process.processType>
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
