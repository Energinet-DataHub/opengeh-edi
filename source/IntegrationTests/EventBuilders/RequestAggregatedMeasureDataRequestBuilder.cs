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
        string? gridArea,
        ActorNumber? energySupplier,
        ActorNumber? balanceResponsibleParty,
        string transactionId)
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
                gridArea,
                energySupplier,
                balanceResponsibleParty,
                transactionId);
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported document format");
        }

        // else if (format == DocumentFormat.Xml)
        // {
        //     content = GetCimXml(
        //         senderActorNumber,
        //         senderActorRole,
        //         meteringPointType,
        //         settlementMethod,
        //         periodStart,
        //         periodEnd,
        //         gridArea,
        //         energySupplier,
        //         balanceResponsibleParty,
        //         transactionId);
        // }

        return new IncomingMessageStream(new MemoryStream(Encoding.UTF8.GetBytes(content)));
    }

    private static string GetCimJson(
        ActorNumber senderActorNumber,
        ActorRole senderActorRole,
        MeteringPointType meteringPointType,
        SettlementMethod settlementMethod,
        Instant periodStart,
        Instant periodEnd,
        string? gridArea,
        ActorNumber? energySupplierActorNumber,
        ActorNumber? balanceResponsiblePartyActorNumber,
        string transactionId)
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
			{{
				""mRID"": ""{transactionId}"",
                {GetBalanceResponsiblePartySection(balanceResponsiblePartyActorNumber?.Value)}
				""end_DateAndOrTime.dateTime"": ""{periodEnd}"",
                {GetEnergySupplierSection(energySupplierActorNumber?.Value)}
				""marketEvaluationPoint.settlementMethod"": {{
					""value"": ""{settlementMethod.Code}""
				}},
				""marketEvaluationPoint.type"": {{
					""value"": ""{meteringPointType.Code}""
				}},
				{GetGridAreaSection(gridArea)}
				""start_DateAndOrTime.dateTime"": ""{periodStart}""
			}}
	    ]
	}}
}}";
    }

    private static string GetBalanceResponsiblePartySection(string? balanceResponsibleParty)
    {
        return balanceResponsibleParty != null
            ? $@"   ""balanceResponsibleParty_MarketParticipant.mRID"": {{
					""codingScheme"": ""A10"",
					""value"": ""{balanceResponsibleParty}""
				}},"
            : string.Empty;
    }

    private static string GetGridAreaSection(string? gridArea)
    {
        return gridArea is not null
            ? $@"				""meteringGridArea_Domain.mRID"": {{
				  ""codingScheme"": ""NDK"",
				  ""value"": ""{gridArea}""
				}},"
            : string.Empty;
    }

    private static string GetEnergySupplierSection(string? energySupplier)
    {
        return energySupplier != null
            ? $@"   ""energySupplier_MarketParticipant.mRID"": {{
            ""codingScheme"": ""A10"",
            ""value"": ""{energySupplier}""
        }},"
        : string.Empty;
    }
}
