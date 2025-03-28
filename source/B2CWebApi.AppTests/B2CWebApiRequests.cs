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

using System.Text.Json;
using Energinet.DataHub.EDI.B2CWebApi.Models;
using NodaTime;

namespace Energinet.DataHub.EDI.B2CWebApi.AppTests;

public static class B2CWebApiRequests
{
    public static HttpRequestMessage CreateArchivedMessageGetDocumentRequest()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/ArchivedMessageGetDocument?id={Guid.NewGuid()}");
        return request;
    }

    public static HttpRequestMessage CreateArchivedMessageSearchV2Request()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/ArchivedMessageSearch?api-version=2")
        {
            Content = CreateJsonContent(
                new SearchArchivedMessagesRequest(
                    new SearchArchivedMessagesCriteria(
                        CreatedDuringPeriod: null,
                        MessageId: null,
                        SenderNumber: null,
                        ReceiverNumber: null,
                        DocumentTypes: null,
                        BusinessReasons: null),
                    new SearchArchivedMessagesPagination())),
        };
        return request;
    }

    public static HttpRequestMessage CreateArchivedMessageSearchV3Request()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/ArchivedMessageSearch?api-version=3")
        {
            Content = CreateJsonContent(
                new SearchArchivedMessagesRequest(
                    new SearchArchivedMessagesCriteria(
                        CreatedDuringPeriod: null,
                        MessageId: null,
                        SenderNumber: null,
                        ReceiverNumber: null,
                        DocumentTypes: null,
                        BusinessReasons: null),
                    new SearchArchivedMessagesPagination())),
        };
        return request;
    }

    public static HttpRequestMessage CreateOrchestrationsRequest()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/Orchestrations?from=2024-09-03T13:37:00");
        return request;
    }

    public static HttpRequestMessage CreateOrchestrationRequest()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/Orchestrations/{Guid.NewGuid()}");
        return request;
    }

    public static HttpRequestMessage CreateOrchestrationTerminateRequest()
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/Orchestrations/{Guid.NewGuid()}/terminate?reason=\"Reason\"");
        return request;
    }

    public static HttpRequestMessage CreateRequestAggregatedMeasureDataRequest()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/RequestAggregatedMeasureData")
        {
            Content = CreateJsonContent(
                new RequestAggregatedMeasureDataMarketRequest(
                    CalculationType: CalculationType.BalanceFixing,
                    MeteringPointType: null,
                    StartDate: "2024-08-27T00:00:00Z",
                    EndDate: "2024-08-28T00:00:00Z",
                    GridArea: null,
                    EnergySupplierId: null,
                    BalanceResponsibleId: null)),
        };
        return request;
    }

    public static HttpRequestMessage CreateRequestWholesaleSettlementRequest()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/RequestWholesaleSettlement")
        {
            Content = CreateJsonContent(
                new RequestWholesaleSettlementMarketRequest(
                    CalculationType: CalculationType.WholesaleFixing,
                    StartDate: "2024-08-27T00:00:00Z",
                    EndDate: "2024-08-28T00:00:00Z",
                    GridArea: null,
                    EnergySupplierId: null,
                    Resolution: null,
                    PriceType: null)),
        };
        return request;
    }

    public static HttpRequestMessage CreateRequestWholesaleSettlementTempRequest()
    {
        var requestWholesaleSettlementChargeTypeV2 = new RequestWholesaleSettlementChargeTypeV2(null, "23");
        var requestWholesaleSettlementChargeTypeV2s = new List<RequestWholesaleSettlementChargeTypeV2> { requestWholesaleSettlementChargeTypeV2 };
        var currentInstant = SystemClock.Instance.GetCurrentInstant();
        var requestWholesaleSettlementSeriesV2 = new EDI.B2CWebApi.Models.RequestWholesaleSettlementSeriesV2(
            Guid.NewGuid().ToString(),
            currentInstant.ToString(),
            currentInstant.Plus(Duration.FromDays(30)).ToString(),
            "804",
            null,
            "D01",
            "PT15M",
            null,
            requestWholesaleSettlementChargeTypeV2s);

        var requestWholesaleServicesMarketDocumentV2 =
            new EDI.B2CWebApi.Models.RequestWholesaleServicesMarketDocumentV2(
                "D05",
                new List<RequestWholesaleSettlementSeriesV2> { requestWholesaleSettlementSeriesV2, });

        var request = new HttpRequestMessage(HttpMethod.Post, "/TempRequestWholesaleSettlement")
        {
            Content = CreateJsonContent(requestWholesaleServicesMarketDocumentV2),
        };
        return request;
    }

    private static StringContent CreateJsonContent(object payload)
    {
        var serializedObject = JsonSerializer.Serialize(payload);
        var stringContent = new StringContent(
            serializedObject,
            System.Text.Encoding.UTF8,
            "application/json");

        return stringContent;
    }
}
