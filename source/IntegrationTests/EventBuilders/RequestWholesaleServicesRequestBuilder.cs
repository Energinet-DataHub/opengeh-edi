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

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;

namespace Energinet.DataHub.EDI.IntegrationTests.EventBuilders;

[SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Used only in tests")]
internal static class RequestWholesaleServicesRequestBuilder
{
    public static IncomingMessageStream GetJsonStream(
        string senderActorNumber,
        string senderActorRole,
        string periodStart,
        string periodEnd,
        string? gridArea,
        string energySupplierActorNumber,
        string transactionId)
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
                        energySupplierActorNumber,
                        transactionId))));
    }

    private static string GetJson(
        string senderActorNumber,
        string senderActorRole,
        string periodStart,
        string periodEnd,
        string? gridArea,
        string energySupplierActorNumber,
        string transactionId)
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
        ""chargeTypeOwner_MarketParticipant.mRID"": {{
          ""codingScheme"": ""A10"",
          ""value"": ""5790001330552""
        }},
        ""end_DateAndOrTime.dateTime"": ""{periodEnd}"",
        ""energySupplier_MarketParticipant.mRID"": {{
          ""codingScheme"": ""A10"",
          ""value"": ""{energySupplierActorNumber}""
        }},
        ""meteringGridArea_Domain.mRID"": {{
          ""codingScheme"": ""NDK"",
          ""value"": ""{gridArea}""
        }},
        ""start_DateAndOrTime.dateTime"": ""{periodStart}"",
        ""ChargeType"": [
          {{
            ""mRID"": ""40000"",
            ""type"": {{
              ""value"": ""D03""
            }}
          }}
        ]
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