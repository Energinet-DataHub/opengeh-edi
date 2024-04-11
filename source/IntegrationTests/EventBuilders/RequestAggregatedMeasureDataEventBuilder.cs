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
using System.IO;
using System.Text;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;

namespace Energinet.DataHub.EDI.IntegrationTests.EventBuilders;

[SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Used only in tests")]
internal static class RequestAggregatedMeasureDataEventBuilder
{
    public static IncomingMessageStream GetJsonStream(
        string senderActorNumber,
        string senderActorRole,
        string periodStart,
        string periodEnd,
        string? gridArea,
        string energySupplierActorNumber = "5799999933318")
    {
        return new IncomingMessageStream(
            new MemoryStream(
                Encoding.UTF8.GetBytes(
                    GetJson(
                        senderActorNumber,
                        senderActorRole,
                        periodStart,
                        periodEnd,
                        gridArea,
                        energySupplierActorNumber))));
    }

    private static string GetJson(
        string senderActorNumber,
        string senderActorRole,
        string periodStart,
        string periodEnd,
        string? gridArea,
        string energySupplierActorNumber = "5799999933318")
    {
        return $@"{{
	""RequestAggregatedMeasureData_MarketDocument"": {{
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
			""value"": ""E74""
		}},
		""Series"": [
			{{
				""mRID"": ""123564789123564789123564789123564787"",
				""balanceResponsibleParty_MarketParticipant.mRID"": {{
					""codingScheme"": ""A10"",
					""value"": ""5799999933318""
				}},
				""end_DateAndOrTime.dateTime"": ""{periodEnd}"",
				""energySupplier_MarketParticipant.mRID"": {{
					""codingScheme"": ""A10"",
					""value"": ""{energySupplierActorNumber}""
				}},
				""marketEvaluationPoint.settlementMethod"": {{
					""value"": ""D01""
				}},
				""marketEvaluationPoint.type"": {{
					""value"": ""E17""
				}},
				{GetGridAreaSection(gridArea)}
				""start_DateAndOrTime.dateTime"": ""{periodStart}"",
				""settlement_Series.version"": {{
	                ""value"": ""D01""
				}}
			}}
	    ]
	}}
}}";
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
}
