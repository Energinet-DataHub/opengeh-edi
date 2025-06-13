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

namespace Energinet.DataHub.EDI.IntegrationTests.Migration;

public static class XmlMessageConstants
{
    public const string PeekMessageContainingTwoTransactions = """
       <?xml version="1.0" encoding="UTF-8"?>
       <SOAP-ENV:Envelope xmlns:SOAP-ENV="http://schemas.xmlsoap.org/soap/envelope/">
        <SOAP-ENV:Body> <ns0:PeekMessageResponse xmlns:ns0="urn:www:datahub:dk:b2b:v01">
         <ns0:MessageContainer>
          <ns0:MessageReference>6cdd5cf318f04b99a785ee975ccc1a29</ns0:MessageReference>
           <ns0:DocumentType>MeteredDataTimeSeriesDH3</ns0:DocumentType>
            <ns0:MessageType>JSON</ns0:MessageType>
             <ns0:Payload>
              <ns0:CData>{ "MeteredDataTimeSeriesDH3":{ "Header":{ "MessageId":"113899308", "Creation":"2025-06-13T10:21:40Z", "EnergyBusinessProcess":"E23", "SenderIdentification":"5790001330552", "RecipientIdentification":"45V000000000183M", "EnergyBusinessProcessRole":"D3M" }, "TimeSeries":[ { "TimeSeriesId":"4547205116_5305560545", "OriginalMessageId":"bf2e186b60014531bf52927dca9ab123", "OriginalTimeSeriesId":"EH_1952236801", "EnergyTimeSeriesMeasureUnit":"KWH", "TypeOfMP":"D14", "AggregationCriteria":{ "MeteringPointId":"571313124590127032" }, "Observation":[ { "Position":1, "QuantityQuality":"D01", "EnergyQuantity":2.737 }, { "Position":2, "QuantityQuality":"D01", "EnergyQuantity":0.0 }, { "Position":3, "QuantityQuality":"D01", "EnergyQuantity":0.0 }, { "Position":4, "QuantityQuality":"D01", "EnergyQuantity":0.0 }, { "Position":5, "QuantityQuality":"D01", "EnergyQuantity":0.0 }, { "Position":6, "QuantityQuality":"D01", "EnergyQuantity":0.0 }, { "Position":7, "QuantityQuality":"D01", "EnergyQuantity":0.0 }, { "Position":8, "QuantityQuality":"D01", "EnergyQuantity":0.0 }, { "Position":9, "QuantityQuality":"D01", "EnergyQuantity":0.0 }, { "Position":10, "QuantityQuality":"D01", "EnergyQuantity":0.0 }, { "Position":11, "QuantityQuality":"D01", "EnergyQuantity":0.0 }, { "Position":12, "QuantityQuality":"D01", "EnergyQuantity":0.0 }, { "Position":13, "QuantityQuality":"D01", "EnergyQuantity":0.0 }, { "Position":14, "QuantityQuality":"D01", "EnergyQuantity":0.0 }, { "Position":15, "QuantityQuality":"D01", "EnergyQuantity":0.0 }, { "Position":16, "QuantityQuality":"D01", "EnergyQuantity":0.0 }, { "Position":17, "QuantityQuality":"D01", "EnergyQuantity":0.0 }, { "Position":18, "QuantityQuality":"D01", "EnergyQuantity":0.0 }, { "Position":19, "QuantityQuality":"D01", "EnergyQuantity":0.0 }, { "Position":20, "QuantityQuality":"D01", "EnergyQuantity":0.0 }, { "Position":21, "QuantityQuality":"D01", "EnergyQuantity":0.0 }, { "Position":22, "QuantityQuality":"D01", "EnergyQuantity":0.0 }, { "Position":23, "QuantityQuality":"D01", "EnergyQuantity":0.0 }, { "Position":24, "QuantityQuality":"D01", "EnergyQuantity":0.0 } ], "TimeSeriesPeriod":{ "ResolutionDuration":"PT1H", "Start":"2025-05-04T22:00:00Z", "End":"2025-05-05T22:00:00Z" }, "TransactionInsertDate":"2025-05-22T13:11:02Z", "TimeSeriesStatus":"2" }, { "TimeSeriesId":"4547206155_5305562851", "OriginalMessageId":"bf2e186b60014531bf52927dca9ab123", "OriginalTimeSeriesId":"EH_1952225841", "EnergyTimeSeriesMeasureUnit":"KWH", "TypeOfMP":"D14", "AggregationCriteria":{ "MeteringPointId":"571313134490664582" }, "Observation":[ { "Position":1, "QuantityQuality":"D01", "EnergyQuantity":2.737 }, { "Position":2, "QuantityQuality":"D01", "EnergyQuantity":0.0 }, { "Position":3, "QuantityQuality":"D01", "EnergyQuantity":0.0 }, { "Position":4, "QuantityQuality":"D01", "EnergyQuantity":0.0 }, { "Position":5, "QuantityQuality":"D01", "EnergyQuantity":0.0 }, { "Position":6, "QuantityQuality":"D01", "EnergyQuantity":0.0 }, { "Position":7, "QuantityQuality":"D01", "EnergyQuantity":0.0 }, { "Position":8, "QuantityQuality":"D01", "EnergyQuantity":0.0 }, { "Position":9, "QuantityQuality":"D01", "EnergyQuantity":0.0 }, { "Position":10, "QuantityQuality":"D01", "EnergyQuantity":0.0 }, { "Position":11, "QuantityQuality":"D01", "EnergyQuantity":0.0 }, { "Position":12, "QuantityQuality":"D01", "EnergyQuantity":0.0 }, { "Position":13, "QuantityQuality":"D01", "EnergyQuantity":0.0 }, { "Position":14, "QuantityQuality":"D01", "EnergyQuantity":0.0 }, { "Position":15, "QuantityQuality":"D01", "EnergyQuantity":0.0 }, { "Position":16, "QuantityQuality":"D01", "EnergyQuantity":0.0 }, { "Position":17, "QuantityQuality":"D01", "EnergyQuantity":0.0 }, { "Position":18, "QuantityQuality":"D01", "EnergyQuantity":0.0 }, { "Position":19, "QuantityQuality":"D01", "EnergyQuantity":0.0 }, { "Position":20, "QuantityQuality":"D01", "EnergyQuantity":0.0 }, { "Position":21, "QuantityQuality":"D01", "EnergyQuantity":0.0 }, { "Position":22, "QuantityQuality":"D01", "EnergyQuantity":0.0 }, { "Position":23, "QuantityQuality":"D01", "EnergyQuantity":0.0 }, { "Position":24, "QuantityQuality":"D01", "EnergyQuantity":0.0 } ], "TimeSeriesPeriod":{ "ResolutionDuration":"PT1H", "Start":"2025-04-14T22:00:00Z", "End":"2025-04-15T22:00:00Z" }, "TransactionInsertDate":"2025-05-22T13:11:02Z", "TimeSeriesStatus":"2" } ] } }</ns0:CData>
             </ns0:Payload>
           </ns0:MessageContainer>
         </ns0:PeekMessageResponse>
        </SOAP-ENV:Body>
       </SOAP-ENV:Envelope>
       """;

    public const string PeekMessageWithoutPayload = """
      <?xml version="1.0" encoding="UTF-8"?>
      <SOAP-ENV:Envelope xmlns:SOAP-ENV="http://schemas.xmlsoap.org/soap/envelope/">
       <SOAP-ENV:Body>
        <ns0:PeekMessageResponse xmlns:ns0="urn:www:datahub:dk:b2b:v01">
         <ns0:MessageContainer>
          <ns0:MessageReference>7f071bd47baa493488aea08f41efcc08</ns0:MessageReference>
          <ns0:DocumentType>MeteredDataTimeSeriesDH3</ns0:DocumentType>
          <ns0:MessageType>JSON</ns0:MessageType>
         </ns0:MessageContainer>
        </ns0:PeekMessageResponse>
       </SOAP-ENV:Body>
      </SOAP-ENV:Envelope>
      """;
}
