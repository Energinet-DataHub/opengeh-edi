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

using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Xml;
using Energinet.DataHub.EDI.B2BApi.AppTests.DurableTask;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.SubsystemTests.Exceptions;
using Energinet.DataHub.EDI.SubsystemTests.Logging;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Nito.AsyncEx;
using NodaTime;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.SubsystemTests.Drivers;

internal sealed class EdiDriver
{
    private readonly IDurableClient _durableClient;
    private readonly AsyncLazy<HttpClient> _httpClient;
    private readonly ITestOutputHelper _logger;

    public EdiDriver(IDurableClient durableClient, AsyncLazy<HttpClient> b2bHttpClient, ITestOutputHelper logger)
    {
        _durableClient = durableClient;
        _httpClient = b2bHttpClient;
        _logger = logger;
    }

    internal async Task<(HttpResponseMessage PeekResponse, HttpResponseMessage DequeueResponse)> PeekMessageAsync(
        DocumentFormat? documentFormat = null)
    {
        var stopWatch = Stopwatch.StartNew();

        // Set timeout to above 20 seconds since internal commands must be handled (twice) before accepted/rejected messages are available
        var timeoutAfter = TimeSpan.FromMinutes(1);
        while (stopWatch.ElapsedMilliseconds < timeoutAfter.TotalMilliseconds)
        {
            var peekResponse = await PeekAsync(documentFormat).ConfigureAwait(false);

            if (peekResponse.StatusCode == HttpStatusCode.OK)
            {
                var dequeueResponse = await DequeueAsync(GetMessageId(peekResponse)).ConfigureAwait(false);
                return (peekResponse, dequeueResponse);
            }

            if (peekResponse.StatusCode != HttpStatusCode.NoContent)
            {
                throw new UnexpectedPeekResponseException($"Unexpected Peek response: {peekResponse.StatusCode}");
            }

            await Task.Delay(500).ConfigureAwait(false);
        }

        throw new TimeoutException("Unable to retrieve peek result within time limit");
    }

    internal async Task<IReadOnlyCollection<HttpResponseMessage>> PeekAllMessagesAsync(DocumentFormat? documentFormat = null)
    {
        // We use PeekAsync() directly since we don't want to wait for messages to become available (which PeekMessageAsync does)
        var peekResponse = await PeekAsync(documentFormat);

        if (peekResponse.StatusCode == HttpStatusCode.NoContent)
        {
            // Received no content - break out of recursive loop and return the found results
            return [];
        }

        if (peekResponse.StatusCode != HttpStatusCode.OK)
            throw new InvalidOperationException($"Unknown http status code while peeking messages: {peekResponse.StatusCode}");

        var results = new List<HttpResponseMessage> { peekResponse };

        await DequeueAsync(GetMessageId(peekResponse)).ConfigureAwait(false);

        results.AddRange(await PeekAllMessagesAsync(documentFormat).ConfigureAwait(false));

        return results;
    }

    internal async Task EmptyQueueAsync()
    {
        var peekResponse = await PeekAsync()
                .ConfigureAwait(false);
        if (peekResponse.StatusCode == HttpStatusCode.OK)
        {
            await DequeueAsync(GetMessageId(peekResponse)).ConfigureAwait(false);
            await EmptyQueueAsync().ConfigureAwait(false);
        }
    }

    internal async Task<string> RequestAggregatedMeasureDataXmlAsync(XmlDocument payload, string? token = null)
    {
        var b2bClient = await _httpClient;
        using var request = new HttpRequestMessage(HttpMethod.Post, "v1.0/cim/requestaggregatedmeasuredata");
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);
        request.Content = new StringContent(payload.OuterXml, Encoding.UTF8, "application/xml");
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/xml");
        var response = await b2bClient.SendAsync(request).ConfigureAwait(false);
        var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        return responseString;
    }

    internal async Task<Guid> RequestWholesaleSettlementAsync(bool withSyncError, CancellationToken cancellationToken)
    {
        var b2bClient = await _httpClient;
        using var request = new HttpRequestMessage(HttpMethod.Post, "v1.0/cim/requestwholesalesettlement");
        var contentType = "application/json";
        var requestContent = await GetRequestWholesaleSettlementContentAsync(withSyncError, cancellationToken)
            .ConfigureAwait(false);
        request.Content = new StringContent(
            requestContent.Content,
            Encoding.UTF8,
            contentType);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        var wholesaleSettlementResponse = await b2bClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (wholesaleSettlementResponse.StatusCode == HttpStatusCode.BadRequest)
        {
            var responseContent = await wholesaleSettlementResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new BadWholesaleSettlementRequestException($"responseContent: {responseContent}");
        }

        await wholesaleSettlementResponse.EnsureSuccessStatusCodeWithLogAsync(_logger);
        return requestContent.MessageId;
    }

    internal async Task<Guid> RequestAggregatedMeasureDataAsync(bool withSyncError, CancellationToken cancellationToken)
    {
        var b2bClient = await _httpClient;
        using var request = new HttpRequestMessage(HttpMethod.Post, "v1.0/cim/requestaggregatedmeasuredata");
        var requestContent = await GetAggregatedMeasureDataContentAsync(withSyncError, cancellationToken).ConfigureAwait(false);
        request.Content = new StringContent(requestContent.Content, Encoding.UTF8, "application/json");
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var aggregatedMeasureDataResponse = await b2bClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (aggregatedMeasureDataResponse.StatusCode == HttpStatusCode.BadRequest)
        {
            var responseContent = await aggregatedMeasureDataResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new BadAggregatedMeasureDataRequestException($"responseContent: {responseContent}");
        }

        await aggregatedMeasureDataResponse.EnsureSuccessStatusCodeWithLogAsync(_logger);
        return requestContent.MessageId;
    }

    internal async Task<DurableOrchestrationStatus> WaitForOrchestrationStartedAsync(Instant orchestrationStartedAfter)
    {
        var orchestration = await _durableClient.WaitForOrchestrationStatusAsync(orchestrationStartedAfter.ToDateTimeUtc());

        return orchestration;
    }

    internal async Task WaitForOrchestrationCompletedAsync(string orchestrationInstanceId)
    {
        await _durableClient.WaitForInstanceCompletedAsync(orchestrationInstanceId, TimeSpan.FromMinutes(30));
    }

    internal async Task StopOrchestrationForCalculationAsync(Guid calculationId, Instant createdAfter)
    {
        var runningOrchestrationsResult = await _durableClient.ListInstancesAsync(
            new OrchestrationStatusQueryCondition
            {
                RuntimeStatus = [
                    OrchestrationRuntimeStatus.Pending,
                    OrchestrationRuntimeStatus.Running,
                ],
                ShowInput = true,
                CreatedTimeFrom = createdAfter.ToDateTimeUtc(),
            },
            CancellationToken.None);

        var orchestrationsForCalculation = runningOrchestrationsResult
            .DurableOrchestrationState
            .Where(o => o.Input.ToString().Contains(calculationId.ToString()))
            .ToList();

        if (!orchestrationsForCalculation.Any())
        {
            _logger.WriteLine($"Found no orchestrations to stop for calculation (CalculationId={calculationId}, CreatedAfter={createdAfter.ToDateTimeUtc()})");
            return;
        }


        foreach (var orchestration in orchestrationsForCalculation)
        {
            _logger.WriteLine($"Stopping orchestration for calculation (CalculationId={calculationId}, OrchestrationInstanceId={orchestration.InstanceId})");
            await _durableClient.TerminateAsync(orchestration.InstanceId, "Stopped after load test");
        }
    }

    private static async Task<(Guid MessageId, string Content)> GetRequestWholesaleSettlementContentAsync(
        bool withSyncError,
        CancellationToken cancellationToken)
    {
        var messageId = Guid.NewGuid();
        var jsonContent = await File.ReadAllTextAsync("Messages/json/RequestWholesaleSettlement.json", cancellationToken)
            .ConfigureAwait(false);

        if (withSyncError is true)
        {
            jsonContent = await File.ReadAllTextAsync("Messages/json/RequestWholesaleSettlementWithSyncError.json", cancellationToken)
                .ConfigureAwait(false);
        }

        jsonContent = jsonContent.Replace("{MessageId}", messageId.ToString(), StringComparison.InvariantCulture);
        jsonContent = jsonContent.Replace("{TransactionId}", Guid.NewGuid().ToString(), StringComparison.InvariantCulture);

        return (messageId, jsonContent);
    }

    private static async Task<(Guid MessageId, string Content)> GetAggregatedMeasureDataContentAsync(bool withSyncError, CancellationToken cancellationToken)
    {
        var messageId = Guid.NewGuid();

        var jsonContent = await File.ReadAllTextAsync("Messages/json/RequestAggregatedMeasureData.json", cancellationToken)
            .ConfigureAwait(false);

        if (withSyncError is true)
        {
            jsonContent = await File.ReadAllTextAsync("Messages/json/RequestAggregatedMeasureDataWithSyncError.json", cancellationToken)
                .ConfigureAwait(false);
        }

        jsonContent = jsonContent.Replace("{MessageId}", messageId.ToString(), StringComparison.InvariantCulture);
        jsonContent = jsonContent.Replace("{TransactionId}", Guid.NewGuid().ToString(), StringComparison.InvariantCulture);

        return (messageId, jsonContent);
    }

    private static string GetMessageId(HttpResponseMessage peekResponse)
    {
        return peekResponse.Headers.GetValues("MessageId").First();
    }

    private async Task<HttpResponseMessage> PeekAsync(DocumentFormat? documentFormat = null)
    {
        var b2bClient = await _httpClient;
        using var request = new HttpRequestMessage(HttpMethod.Get, "v1.0/cim/aggregations");
        var contentType = documentFormat == null || DocumentFormat.Json == documentFormat ? "application/json" : "application/xml";
        request.Content = new StringContent(string.Empty, Encoding.UTF8, contentType);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        var peekResponse = await b2bClient.SendAsync(request).ConfigureAwait(false);
        await peekResponse.EnsureSuccessStatusCodeWithLogAsync(_logger);
        return peekResponse;
    }

    private async Task<HttpResponseMessage> DequeueAsync(string messageId)
    {
        var b2bClient = await _httpClient;
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"v1.0/cim/dequeue/{messageId}");
        var dequeueResponse = await b2bClient.SendAsync(request).ConfigureAwait(false);
        await dequeueResponse.EnsureSuccessStatusCodeWithLogAsync(_logger);

        return dequeueResponse;
    }
}
