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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
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
        ActorNumber? energySupplierActorNumber,
        ActorNumber? chargeOwnerActorNumber,
        string? chargeCode,
        ChargeType? chargeType,
        bool isMonthly,
        IReadOnlyCollection<(string? GridArea, string TransactionId)> series)
    {
        string content;
        if (format == DocumentFormat.Json)
        {
            content = GetCimJson(
                senderActorNumber.Value,
                senderActorRole.Code,
                periodStart.ToString(),
                periodEnd.ToString(),
                energySupplierActorNumber?.Value,
                chargeOwnerActorNumber?.Value,
                chargeCode,
                chargeType?.Code,
                isMonthly,
                series);
        }
        else if (format == DocumentFormat.Xml)
        {
            content = GetCimXml(
                senderActorNumber.Value,
                senderActorRole.Code,
                periodStart.ToString(),
                periodEnd.ToString(),
                energySupplierActorNumber?.Value,
                chargeOwnerActorNumber?.Value,
                chargeCode,
                chargeType?.Code,
                isMonthly,
                series);
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
        string? energySupplierActorNumber,
        string? chargeOwnerActorNumber,
        string? chargeCode,
        string? chargeType,
        bool isMonthly,
        IReadOnlyCollection<(string? GridArea, string TransactionId)> series)
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
        {string.Join(",\n", series.Select(s => $@"
        {{
            ""mRID"": ""{s.TransactionId}"",
            {GetCimJsonResolutionSection(isMonthly)}
            {GetCimJsonChargeOwnerSection(chargeOwnerActorNumber)}
            ""end_DateAndOrTime.dateTime"": ""{periodEnd}"",
            {GetCimJsonEnergySupplierSection(energySupplierActorNumber)}
            {GetCimJsonGridAreaElement(s.GridArea)}
            ""start_DateAndOrTime.dateTime"": ""{periodStart}"",
            ""ChargeType"": {GetCimJsonChargeTypeSection(new() { (chargeCode, chargeType) })}
        }}"))}
    ]
  }}
}}";
    }

    private static string GetCimJsonChargeOwnerSection(string? chargeOwnerActorNumber)
    {
        return chargeOwnerActorNumber != null
            ? $@"
                ""chargeTypeOwner_MarketParticipant.mRID"": {{
                    ""codingScheme"": ""A10"",
                    ""value"": ""{chargeOwnerActorNumber}""
                }},"
            : string.Empty;
    }

    private static string GetCimJsonChargeTypeSection(List<(string? ChargeCode, string? ChargeType)> chargeTypes)
    {
        var array = "[";

        array += string.Join(",\n", chargeTypes
            .Where(c => c.ChargeCode != null || c.ChargeType != null)
            .Select((c) => $@"
                {{
                    ""mRID"": ""{c.ChargeCode}"",
                    ""type"": {{
                        ""value"": ""{c.ChargeType}""
                    }}
                }}"));

        array += "]";

        return array;
    }

    private static string GetCimJsonEnergySupplierSection(string? energySupplierActorNumber)
    {
        return energySupplierActorNumber != null
            ? $@"
                ""energySupplier_MarketParticipant.mRID"": {{
                    ""codingScheme"": ""A10"",
                    ""value"": ""{energySupplierActorNumber}""
                }},"
            : string.Empty;
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
        string? energySupplierActorNumber,
        string? chargeOwnerActorNumber,
        string? chargeCode,
        string? chargeType,
        bool isMonthly,
        IReadOnlyCollection<(string? GridArea, string TransactionId)> series)
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
    {string.Join("\n", series.Select(s => $@"
        <cim:Series>
		    <cim:mRID>{s.TransactionId}</cim:mRID>
		    <cim:start_DateAndOrTime.dateTime>{periodStart}</cim:start_DateAndOrTime.dateTime>
		    <cim:end_DateAndOrTime.dateTime>{periodEnd}</cim:end_DateAndOrTime.dateTime>
		    {GetCimXmlGridAreaSection(s.GridArea)}
            {GetCimXmlEnergySupplierSection(energySupplierActorNumber)}
            {GetCimXmlChargeOwnerSection(chargeOwnerActorNumber)}
		    {GetCimXmlResolutionSection(isMonthly)}
            {GetCimXmlChargeTypesSection(new() { (chargeType, chargeCode) })}
	    </cim:Series>"))}
</cim:RequestWholesaleSettlement_MarketDocument>";
    }

    private static string GetCimXmlChargeTypesSection(List<(string? ChargeType, string? ChargeCode)> chargeTypes)
    {
        return string.Join("\n", chargeTypes
            .Where(c => c.ChargeCode != null || c.ChargeType != null)
            .Select(c => @$"
                <cim:ChargeType>
			        <cim:type>{c.ChargeType}</cim:type>
			        <cim:mRID>{c.ChargeCode}</cim:mRID>
		        </cim:ChargeType>
            "));
    }

    private static string GetCimXmlChargeOwnerSection(string? chargeOwnerActorNumber)
    {
        return chargeOwnerActorNumber != null
            ? $"<cim:chargeTypeOwner_MarketParticipant.mRID codingScheme=\"A10\">{{chargeOwnerActorNumber}}</cim:chargeTypeOwner_MarketParticipant.mRID>"
            : string.Empty;
    }

    private static string GetCimXmlEnergySupplierSection(string? energySupplierActorNumber)
    {
        return energySupplierActorNumber != null
            ? $"<cim:energySupplier_MarketParticipant.mRID codingScheme=\"A10\">{energySupplierActorNumber}</cim:energySupplier_MarketParticipant.mRID>"
            : string.Empty;
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
