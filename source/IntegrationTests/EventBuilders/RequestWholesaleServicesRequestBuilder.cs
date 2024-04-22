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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using NodaTime;

namespace Energinet.DataHub.EDI.IntegrationTests.EventBuilders;

[SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Used only in tests")]
internal static class RequestWholesaleServicesRequestBuilder
{
    /// <summary>
    /// Create a stream containing a RequestWholesaleSettlement message in JSON format (not monthly)
    /// </summary>
    public static IncomingMessageStream GetJsonStream(
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
        return new IncomingMessageStream(
            new MemoryStream(
                Encoding.UTF8.GetBytes(
                    GetJson(
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
                        isMonthly))));
    }

    private static string GetJson(
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
        {GetResolutionSection(isMonthly)}
        ""chargeTypeOwner_MarketParticipant.mRID"": {{
          ""codingScheme"": ""A10"",
          ""value"": ""{chargeOwnerActorNumber}""
        }},
        ""end_DateAndOrTime.dateTime"": ""{periodEnd}"",
        ""energySupplier_MarketParticipant.mRID"": {{
          ""codingScheme"": ""A10"",
          ""value"": ""{energySupplierActorNumber}""
        }},
        {GetGridAreaElement(gridArea)}
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

    private static string GetResolutionSection(bool isMonthly)
    {
        return isMonthly
            ? """
              "aggregationSeries_Period.resolution": "P1M",
              """
            : string.Empty;
    }

    private static string GetGridAreaElement(string? gridArea)
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
}
