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
internal static class RequestAggregatedMeasureDataRequestBuilder
{
    public static IncomingMessageStream GetStream(
        DocumentFormat format,
        ActorNumber senderActorNumber,
        ActorRole senderActorRole,
        MeteringPointType meteringPointType,
        SettlementMethod settlementMethod,
        Instant periodStart,
        Instant periodEnd,
        ActorNumber? energySupplier,
        ActorNumber? balanceResponsibleParty,
        IReadOnlyCollection<(string? GridArea, string TransactionId)> series)
    {
        string content;
        if (format == DocumentFormat.Json)
        {
            content = GetCimJson(
                senderActorNumber,
                senderActorRole,
                meteringPointType,
                settlementMethod,
                periodStart,
                periodEnd,
                energySupplier,
                balanceResponsibleParty,
                series);
        }
        else if (format == DocumentFormat.Xml)
        {
            content = GetCimXml(
                senderActorNumber,
                senderActorRole,
                meteringPointType,
                settlementMethod,
                periodStart,
                periodEnd,
                energySupplier,
                balanceResponsibleParty,
                series);
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported document format");
        }

        return new IncomingMessageStream(new MemoryStream(Encoding.UTF8.GetBytes(content)));
    }

    private static string GetCimXml(
        ActorNumber senderActorNumber,
        ActorRole senderActorRole,
        MeteringPointType meteringPointType,
        SettlementMethod settlementMethod,
        Instant periodStart,
        Instant periodEnd,
        ActorNumber? energySupplier,
        ActorNumber? balanceResponsibleParty,
        IReadOnlyCollection<(string? GridArea, string TransactionId)> series)
    {
        return $@"<cim:RequestAggregatedMeasureData_MarketDocument xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:cim=""urn:ediel.org:measure:requestaggregatedmeasuredata:0:1"" xsi:schemaLocation=""urn:ediel.org:measure:requestaggregatedmeasuredata:0:1 urn-ediel-org-measure-requestaggregatedmeasuredata-0-1.xsd"">
    <cim:mRID>123564789123564789123564789123564789</cim:mRID>
    <cim:type>E74</cim:type>
    <cim:process.processType>D04</cim:process.processType>
    <cim:businessSector.type>23</cim:businessSector.type>
    <cim:sender_MarketParticipant.mRID codingScheme=""A10"">{senderActorNumber.Value}</cim:sender_MarketParticipant.mRID>
    <cim:sender_MarketParticipant.marketRole.type>{senderActorRole.Code}</cim:sender_MarketParticipant.marketRole.type>
    <cim:receiver_MarketParticipant.mRID codingScheme=""A10"">5790001330552</cim:receiver_MarketParticipant.mRID>
    <cim:receiver_MarketParticipant.marketRole.type>DGL</cim:receiver_MarketParticipant.marketRole.type>
    <cim:createdDateTime>2022-12-17T09:30:47Z</cim:createdDateTime>
    {string.Join("\n", series.Select(s => $@"
        <cim:Series>
            <cim:mRID>{s.TransactionId}</cim:mRID>
            <!-- <cim:settlement_Series.version>D01</cim:settlement_Series.version> -->
            <cim:marketEvaluationPoint.type>{meteringPointType.Code}</cim:marketEvaluationPoint.type>
            <cim:marketEvaluationPoint.settlementMethod>{settlementMethod.Code}</cim:marketEvaluationPoint.settlementMethod>
            <cim:start_DateAndOrTime.dateTime>{periodStart.ToString()}</cim:start_DateAndOrTime.dateTime>
            <cim:end_DateAndOrTime.dateTime>{periodEnd.ToString()}</cim:end_DateAndOrTime.dateTime>
		    {GetGridAreaCimXmlSection(s.GridArea)}
            <cim:biddingZone_Domain.mRID codingScheme=""A01"">10YDK-1--------M</cim:biddingZone_Domain.mRID> <!-- anvendes ikke -->
            {GetEnergySupplierCimXmlSection(energySupplier?.Value)}
            {GetBalanceResponsibleCimXmlSection(balanceResponsibleParty?.Value)}
        </cim:Series>"))}
</cim:RequestAggregatedMeasureData_MarketDocument>";
    }

    private static string GetEnergySupplierCimXmlSection(string? energySupplier)
    {
        return energySupplier != null
            ? $"<cim:energySupplier_MarketParticipant.mRID codingScheme=\"A10\">{energySupplier}</cim:energySupplier_MarketParticipant.mRID>"
            : string.Empty;
    }

    private static string GetBalanceResponsibleCimXmlSection(string? balanceResponsibleParty)
    {
        return balanceResponsibleParty != null
            ? $"<cim:balanceResponsibleParty_MarketParticipant.mRID codingScheme=\"A10\">{balanceResponsibleParty}</cim:balanceResponsibleParty_MarketParticipant.mRID>"
            : string.Empty;
    }

    private static string GetGridAreaCimXmlSection(string? gridArea)
    {
        return gridArea != null
            ? $"<cim:meteringGridArea_Domain.mRID codingScheme=\"NDK\">{gridArea}</cim:meteringGridArea_Domain.mRID>"
            : string.Empty;
    }

    private static string GetCimJson(
        ActorNumber senderActorNumber,
        ActorRole senderActorRole,
        MeteringPointType meteringPointType,
        SettlementMethod settlementMethod,
        Instant periodStart,
        Instant periodEnd,
        ActorNumber? energySupplierActorNumber,
        ActorNumber? balanceResponsiblePartyActorNumber,
        IReadOnlyCollection<(string? GridArea, string TransactionId)> series)
    {
        return $@"{{
	""RequestAggregatedMeasureData_MarketDocument"": {{
		""mRID"": ""123564789123564789123564789123564789"",
		""businessSector.type"": {{
			""value"": ""23""
		}},
		""createdDateTime"": ""2022-12-17T09:30:47Z"",
		""process.processType"": {{
			""value"": ""D04""
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
			""value"": ""{senderActorNumber.Value}""
		}},
		""sender_MarketParticipant.marketRole.type"": {{
			""value"": ""{senderActorRole.Code}""
		}},
		""type"": {{
			""value"": ""E74""
		}},
		""Series"": [
        {string.Join(",\n", series.Select(s => $@"
            {{
				""mRID"": ""{s.TransactionId}"",
                {GetBalanceResponsiblePartyCimJsonSection(balanceResponsiblePartyActorNumber?.Value)}
				""end_DateAndOrTime.dateTime"": ""{periodEnd}"",
                {GetEnergySupplierCimJsonSection(energySupplierActorNumber?.Value)}
				""marketEvaluationPoint.settlementMethod"": {{
					""value"": ""{settlementMethod.Code}""
				}},
				""marketEvaluationPoint.type"": {{
					""value"": ""{meteringPointType.Code}""
				}},
				{GetGridAreaCimJsonSection(s.GridArea)}
				""start_DateAndOrTime.dateTime"": ""{periodStart}""
			}}"))}
	    ]
	}}
}}";
    }

    private static string GetBalanceResponsiblePartyCimJsonSection(string? balanceResponsibleParty)
    {
        return balanceResponsibleParty != null
            ? $@"   ""balanceResponsibleParty_MarketParticipant.mRID"": {{
					""codingScheme"": ""A10"",
					""value"": ""{balanceResponsibleParty}""
				}},"
            : string.Empty;
    }

    private static string GetGridAreaCimJsonSection(string? gridArea)
    {
        return gridArea is not null
            ? $@"				""meteringGridArea_Domain.mRID"": {{
				  ""codingScheme"": ""NDK"",
				  ""value"": ""{gridArea}""
				}},"
            : string.Empty;
    }

    private static string GetEnergySupplierCimJsonSection(string? energySupplier)
    {
        return energySupplier != null
            ? $@"   ""energySupplier_MarketParticipant.mRID"": {{
            ""codingScheme"": ""A10"",
            ""value"": ""{energySupplier}""
        }},"
        : string.Empty;
    }
}
