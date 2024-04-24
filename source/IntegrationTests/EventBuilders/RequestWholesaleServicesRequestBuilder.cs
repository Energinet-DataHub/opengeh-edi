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

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using NodaTime;

namespace Energinet.DataHub.EDI.IntegrationTests.EventBuilders;

[SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Used only in tests")]
internal static class RequestWholesaleServicesRequestBuilder
{
    /// <summary>
    /// Create a stream containing a RequestWholesaleSettlement message in the specified format
    /// </summary>
    public static IncomingMessageStream GetStream(
        DocumentFormat format,
        ActorNumber senderActorNumber,
        ActorRole senderActorRole,
        Instant periodStart,
        Instant periodEnd,
        string? gridArea,
        ActorNumber energySupplierActorNumber,
        ActorNumber chargeOwnerActorNumber,
        string chargeCode,
        ChargeType chargeType,
        string transactionId,
        bool isMonthly)
    {
        string content;
        if (format == DocumentFormat.Json)
        {
            content = GetCimJson(
                senderActorNumber.Value,
                senderActorRole.Code,
                periodStart.ToString(),
                periodEnd.ToString(),
                gridArea,
                energySupplierActorNumber.Value,
                chargeOwnerActorNumber.Value,
                chargeCode,
                chargeType.Code,
                transactionId,
                isMonthly);
        }
        else if (format == DocumentFormat.Xml)
        {
            content = GetCimXml(
                senderActorNumber.Value,
                senderActorRole.Code,
                periodStart.ToString(),
                periodEnd.ToString(),
                gridArea,
                energySupplierActorNumber.Value,
                chargeOwnerActorNumber.Value,
                chargeCode,
                chargeType.Code,
                transactionId,
                isMonthly);
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported document format");
        }

        return new IncomingMessageStream(new MemoryStream(Encoding.UTF8.GetBytes(content)));
    }

    private static string GetCimJson(
        string senderActorNumber,
        string senderActorRole,
        string periodStart,
        string periodEnd,
        string? gridArea,
        string energySupplierActorNumber,
        string chargeOwnerActorNumber,
        string chargeCode,
        string chargeType,
        string transactionId,
        bool isMonthly)
    {
        return $@"{{
  ""RequestWholesaleSettlement_MarketDocument"": {{
    ""mRID"": ""123564789123564789123564789123564789"",
    ""businessSector.type"": {{
      ""value"": ""23""
    }},
    ""createdDateTime"": ""2022-12-17T09:30:47Z"",
    ""process.processType"": {{
      ""value"": ""D05""
    }},
    ""receiver_MarketParticipant.mRID"": {{
      ""codingScheme"": ""A10"",
      ""value"": ""5790001330552""
    }},
    ""receiver_MarketParticipant.marketRole.type"": {{
      ""value"": ""DGL""
    }},
    ""sender_MarketParticipant.mRID"": {{
      ""codingScheme"": ""A10"",
      ""value"": ""{senderActorNumber}""
    }},
    ""sender_MarketParticipant.marketRole.type"": {{
      ""value"": ""{senderActorRole}""
    }},
    ""type"": {{
      ""value"": ""D21""
    }},
    ""Series"": [
      {{
        ""mRID"": ""{transactionId}"",
        {GetCimJsonResolutionSection(isMonthly)}
        ""chargeTypeOwner_MarketParticipant.mRID"": {{
          ""codingScheme"": ""A10"",
          ""value"": ""{chargeOwnerActorNumber}""
        }},
        ""end_DateAndOrTime.dateTime"": ""{periodEnd}"",
        ""energySupplier_MarketParticipant.mRID"": {{
          ""codingScheme"": ""A10"",
          ""value"": ""{energySupplierActorNumber}""
        }},
        {GetCimJsonGridAreaElement(gridArea)}
        ""start_DateAndOrTime.dateTime"": ""{periodStart}"",
        ""ChargeType"": [
          {{
            ""mRID"": ""{chargeCode}"",
            ""type"": {{
              ""value"": ""{chargeType}""
            }}
          }}
        ]
      }}
    ]
  }}
}}";
    }

    private static string GetCimJsonResolutionSection(bool isMonthly)
    {
        return isMonthly
            ? """
              "aggregationSeries_Period.resolution": "P1M",
              """
            : string.Empty;
    }

    private static string GetCimJsonGridAreaElement(string? gridArea)
    {
        return gridArea != null
            ? $$"""
                "meteringGridArea_Domain.mRID": {
                  "codingScheme": "NDK",
                  "value": "{{gridArea}}"
                },
                """
            : string.Empty;
    }

    private static string GetCimXml(
        string senderActorNumber,
        string senderActorRole,
        string periodStart,
        string periodEnd,
        string? gridArea,
        string energySupplierActorNumber,
        string chargeOwnerActorNumber,
        string chargeCode,
        string chargeType,
        string transactionId,
        bool isMonthly)
    {
        return $@"
<?xml version=""1.0"" encoding=""UTF-8""?>
<!--Sample XML file generated by XMLSpy v2021 rel. 3 (x64) (http://www.altova.com)-->
<cim:RequestWholesaleSettlement_MarketDocument xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:cim=""urn:ediel.org:measure:requestwholesalesettlement:0:1"" xsi:schemaLocation=""urn:ediel.org:measure:requestwholesalesettlement:0:1 urn-ediel-org-measure-requestwholesalesettlement-0-1.xsd"">
	<cim:mRID>123564789123564789123564789123564789</cim:mRID>
	<cim:type>D21</cim:type>
	<cim:process.processType>D05</cim:process.processType>
	<cim:businessSector.type>23</cim:businessSector.type>
	<cim:sender_MarketParticipant.mRID codingScheme=""A10"">{senderActorNumber}</cim:sender_MarketParticipant.mRID>
	<cim:sender_MarketParticipant.marketRole.type>{senderActorRole}</cim:sender_MarketParticipant.marketRole.type>
	<cim:receiver_MarketParticipant.mRID codingScheme=""A10"">5790001330552</cim:receiver_MarketParticipant.mRID>
	<cim:receiver_MarketParticipant.marketRole.type>DGL</cim:receiver_MarketParticipant.marketRole.type>
	<cim:createdDateTime>2022-12-17T09:30:47Z</cim:createdDateTime>
	<cim:Series>
		<cim:mRID>{transactionId}</cim:mRID>
		<cim:start_DateAndOrTime.dateTime>{periodStart}</cim:start_DateAndOrTime.dateTime>
		<cim:end_DateAndOrTime.dateTime>{periodEnd}</cim:end_DateAndOrTime.dateTime>
		{GetCimXmlGridAreaSection(gridArea)}
		<cim:energySupplier_MarketParticipant.mRID codingScheme=""A10"">{energySupplierActorNumber}</cim:energySupplier_MarketParticipant.mRID>
		<cim:chargeTypeOwner_MarketParticipant.mRID codingScheme=""A10"">{chargeOwnerActorNumber}</cim:chargeTypeOwner_MarketParticipant.mRID>
		{GetCimXmlResolutionSection(isMonthly)}
		<cim:ChargeType>
			<cim:type>{chargeType}</cim:type>
			<cim:mRID>{chargeCode}</cim:mRID>
		</cim:ChargeType>
	</cim:Series>
</cim:RequestWholesaleSettlement_MarketDocument>";
    }

    private static string GetCimXmlResolutionSection(bool isMonthly)
    {
        return isMonthly
            ? $"<cim:aggregationSeries_Period.resolution>P1M</cim:aggregationSeries_Period.resolution>"
            : string.Empty;
    }

    private static string GetCimXmlGridAreaSection(string? gridArea)
    {
        return gridArea != null
            ? $"<cim:meteringGridArea_Domain.mRID codingScheme=\"NDK\">{gridArea}</cim:meteringGridArea_Domain.mRID>"
            : string.Empty;
    }
}
