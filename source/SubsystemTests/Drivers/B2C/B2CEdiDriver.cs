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

using Energinet.DataHub.EDI.SubsystemTests.Drivers.B2C.Client;
using Energinet.DataHub.EDI.SubsystemTests.Drivers.B2C.ClientV2;
using Energinet.DataHub.EDI.SubsystemTests.Drivers.B2C.ClientV3;
using Nito.AsyncEx;
using NodaTime;
using NodaTime.Text;
using Xunit.Abstractions;
using RequestWholesaleServicesMarketDocumentV2 = Energinet.DataHub.EDI.SubsystemTests.Drivers.B2C.ClientV2.RequestWholesaleServicesMarketDocumentV2;
using RequestWholesaleSettlementChargeTypeV2 = Energinet.DataHub.EDI.SubsystemTests.Drivers.B2C.ClientV2.RequestWholesaleSettlementChargeTypeV2;
using RequestWholesaleSettlementSeriesV2 = Energinet.DataHub.EDI.SubsystemTests.Drivers.B2C.ClientV2.RequestWholesaleSettlementSeriesV2;

namespace Energinet.DataHub.EDI.SubsystemTests.Drivers.B2C;

public sealed class B2CEdiDriver : IDisposable
{
    private readonly AsyncLazy<HttpClient> _httpClient;
    private readonly Uri _apiManagementUri;
    private readonly Uri _b2cWebApiUri;
    private readonly ITestOutputHelper _logger;

    public B2CEdiDriver(AsyncLazy<HttpClient> b2CHttpClient, Uri apiManagementUri, Uri b2cWebApiUri, ITestOutputHelper logger)
    {
        _httpClient = b2CHttpClient;
        _apiManagementUri = apiManagementUri;
        _b2cWebApiUri = b2cWebApiUri;
        _logger = logger;
    }

    public void Dispose()
    {
    }

    public async Task<ArchivedMessageSearchResponse> SearchArchivedMessagesV2Async(SearchArchivedMessagesRequest request)
    {
        var webApiClient = await CreateWebApiClientV2Async();

        var result = await webApiClient.ArchivedMessageSearchAsync(api_version: "2.0", body: request);

        return result;
    }

    public async Task<ArchivedMessageSearchResponseV3> SearchArchivedMessagesV3Async(SearchArchivedMessagesRequestV3 request)
    {
        var webApiClient = await CreateWebApiClientV3Async();

        var result = await webApiClient.ArchivedMessageSearchAsync(api_version: "3.0", body: request);

        return result;
    }

    public async Task RequestAggregatedMeasureDataAsync(CancellationToken cancellationToken)
    {
        var webApiClient = await CreateWebApiClientAsync();

        var start = Instant.FromUtc(2024, 09, 24, 00, 00);
        await webApiClient.RequestAggregatedMeasureDataAsync(
                body: new RequestAggregatedMeasureDataMarketRequest
                {
                    CalculationType = CalculationType.BalanceFixing,
                    StartDate = InstantPattern.General.Format(start),
                    EndDate = InstantPattern.General.Format(start.Plus(Duration.FromDays(1))),
                    GridArea = "804",
                    EnergySupplierId = "5790001330552",
                },
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task RequestWholesaleSettlementAsync(CancellationToken cancellationToken)
    {
        var webApiClient = await CreateWebApiClientAsync();

        var start = Instant.FromUtc(2024, 09, 01, 00, 00);
        await webApiClient.RequestWholesaleSettlementAsync(
                body: new RequestWholesaleSettlementMarketRequest
                {
                    CalculationType = CalculationType.WholesaleFixing,
                    StartDate = InstantPattern.General.Format(start),
                    EndDate = InstantPattern.General.Format(start.Plus(Duration.FromDays(30))),
                    GridArea = "804",
                },
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task RequestWholesaleSettlementV2Async(CancellationToken cancellationToken)
    {
        var webApiClient = await CreateWebApiClientV2Async();

        var start = Instant.FromUtc(2024, 09, 01, 00, 00);
        var requestWholesaleServicesMarketDocumentV2 = new RequestWholesaleServicesMarketDocumentV2
        {
            BusinessReason = "D05",
            BusinessType = "A02",
            MessageId = Guid.NewGuid().ToString(),
            CreatedAt = start.ToString(),
            MessageType = "D21",
            SenderNumber = "5790001330552",
            ReceiverNumber = "5790001330801",
            SenderRoleCode = "DGL",
            ReceiverRoleCode = "DDQ",
            Series = new List<RequestWholesaleSettlementSeriesV2>
            {
                new RequestWholesaleSettlementSeriesV2
                {
                    Id = Guid.NewGuid().ToString(),
                    StartDateAndOrTimeDateTime = start.ToString(),
                    EndDateAndOrTimeDateTime = start.Plus(Duration.FromDays(30)).ToString(),
                    MeteringGridAreaDomainId = "804",
                    EnergySupplierMarketParticipantId = "5790001330552",
                    SettlementVersion = "D01",
                    Resolution = "D01",
                    ChargeOwner = null,
                    ChargeTypes = new List<RequestWholesaleSettlementChargeTypeV2>
                    {
                        new RequestWholesaleSettlementChargeTypeV2
                        {
                            Id = null,
                            Type = "23",
                        },
                    },
                },
            },
        };
        await webApiClient.RequestWholesaleSettlementAsync(
                api_version: "1.0",
                body: requestWholesaleServicesMarketDocumentV2,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<B2CEdiClient> CreateWebApiClientAsync()
    {
        var httpClient = await _httpClient;

        return new B2CEdiClient(_b2cWebApiUri.AbsoluteUri, httpClient);
    }

    private async Task<B2CEdiClientV2> CreateWebApiClientV2Async()
    {
        var httpClient = await _httpClient;

        return new B2CEdiClientV2(_b2cWebApiUri.AbsoluteUri, httpClient);
    }

    private async Task<B2CEdiClientV3> CreateWebApiClientV3Async()
    {
        var httpClient = await _httpClient;

        return new B2CEdiClientV3(_b2cWebApiUri.AbsoluteUri, httpClient);
    }
}
