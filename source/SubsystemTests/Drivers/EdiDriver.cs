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

using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Xml;
using Energinet.DataHub.Core.DurableFunctionApp.TestCommon.DurableTask;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Extensions;
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
    private readonly AsyncLazy<HttpClient> _b2BHttpClient;
    private readonly ITestOutputHelper _logger;

    public EdiDriver(
        IDurableClient durableClient,
        AsyncLazy<HttpClient> b2BHttpClient,
        ITestOutputHelper logger)
    {
        _durableClient = durableClient;
        _b2BHttpClient = b2BHttpClient;
        _logger = logger;
    }

    internal async Task<(HttpResponseMessage PeekResponse, HttpResponseMessage DequeueResponse)> PeekMessageAsync(
        DocumentFormat? documentFormat = null,
        MessageCategory? messageCategory = null,
        TimeSpan? timeout = null)
    {
        var stopWatch = Stopwatch.StartNew();

        var timeoutAfter = timeout ?? TimeSpan.FromMinutes(1);
        while (stopWatch.ElapsedMilliseconds < timeoutAfter.TotalMilliseconds)
        {
            var peekResponse = await PeekAsync(documentFormat, messageCategory).ConfigureAwait(false);

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

    internal async Task EmptyQueueAsync(MessageCategory? messageCategory = null)
    {
        _logger.WriteLine("Emptying actor message queue.");
        var peekResponse = await PeekAsync(messageCategory: messageCategory)
                .ConfigureAwait(false);
        if (peekResponse.StatusCode == HttpStatusCode.OK)
        {
            await DequeueAsync(GetMessageId(peekResponse)).ConfigureAwait(false);
            await EmptyQueueAsync(messageCategory).ConfigureAwait(false);
        }
    }

    internal async Task<string> RequestAggregatedMeasureDataXmlAsync(XmlDocument payload, string? token = null)
    {
        var b2bClient = await _b2BHttpClient;
        using var request = new HttpRequestMessage(HttpMethod.Post, "v1.0/cim/requestaggregatedmeasuredata");
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);
        request.Content = new StringContent(payload.OuterXml, Encoding.UTF8, "application/xml");
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/xml");
        var response = await b2bClient.SendAsync(request).ConfigureAwait(false);
        var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        return responseString;
    }

    internal async Task<string> RequestWholesaleSettlementAsync(bool withSyncError, CancellationToken cancellationToken)
    {
        var b2bClient = await _b2BHttpClient;
        using var request = new HttpRequestMessage(HttpMethod.Post, "v1.0/cim/requestwholesalesettlement");
        var contentType = "application/json";
        var requestContent = await GetRequestWholesaleSettlementContentAsync(withSyncError, cancellationToken)
            .ConfigureAwait(false);
        request.Content = new StringContent(
            requestContent.Content,
            Encoding.UTF8,
            contentType);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        _logger.WriteLine("Sending RequestWholesaleSettlement HTTP request with messageId: {0}", requestContent.MessageId);
        var wholesaleSettlementResponse = await b2bClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (wholesaleSettlementResponse.StatusCode == HttpStatusCode.BadRequest)
        {
            var responseContent = await wholesaleSettlementResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new BadWholesaleSettlementRequestException($"responseContent: {responseContent}");
        }

        await wholesaleSettlementResponse.EnsureSuccessStatusCodeWithLogAsync(_logger);
        return requestContent.MessageId;
    }

    internal async Task<string> SendForwardMeteredDataAsync(
        MeteringPointId meteringPointId,
        DocumentFormat documentFormat,
        CancellationToken cancellationToken)
    {
        var b2bClient = await _b2BHttpClient;
        using var request = new HttpRequestMessage(HttpMethod.Post, "v1.0/cim/notifyvalidatedmeasuredata");
        var contentType = documentFormat switch
        {
            var df when df == DocumentFormat.Json => "application/json",
            var df when df == DocumentFormat.Xml => "application/xml",
            _ => throw new ArgumentOutOfRangeException(nameof(documentFormat), documentFormat, "Unhandled document format"),
        };

        var requestContent = await GetMeteredDataForMeteringPointContentAsync(
                meteringPointId,
                documentFormat,
                cancellationToken)
            .ConfigureAwait(false);

        request.Content = new StringContent(
            requestContent.Content,
            Encoding.UTF8,
            contentType);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        _logger.WriteLine("Sending ForwardMeteredData HTTP request with messageId: {0}", requestContent.MessageId);
        var meteredDataForMeteringPointResponse = await b2bClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (meteredDataForMeteringPointResponse.StatusCode == HttpStatusCode.BadRequest)
        {
            var responseContent = await meteredDataForMeteringPointResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new BadMeteredDataForMeteringPointException($"responseContent: {responseContent}");
        }

        return requestContent.MessageId;
    }

    internal async Task<string> SendRequestMeasurementsAsync(DocumentFormat documentFormat)
    {
        var b2bClient = await _b2BHttpClient;
        using var request = new HttpRequestMessage(HttpMethod.Post, "v1.0/cim/requestvalidatedmeasurements");
        var contentType = documentFormat switch
        {
            var df when df == DocumentFormat.Json => "application/json",
            _ => throw new ArgumentOutOfRangeException(nameof(documentFormat), documentFormat, "Unhandled document format"),
        };

        var requestContent = await GetRequestMeasurementsContentAsync(documentFormat)
            .ConfigureAwait(false);

        request.Content = new StringContent(
            requestContent.Content,
            Encoding.UTF8,
            contentType);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        _logger.WriteLine("Sending RequestMeasurements HTTP request with messageId: {0}", requestContent.MessageId);
        var response = await b2bClient.SendAsync(request).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            throw new BadRequestMeasurementsResponseException($"responseContent: {responseContent}");
        }

        return requestContent.MessageId;
    }

    internal async Task<string> RequestAggregatedMeasureDataAsync(bool withSyncError, CancellationToken cancellationToken)
    {
        var b2bClient = await _b2BHttpClient;
        using var request = new HttpRequestMessage(HttpMethod.Post, "v1.0/cim/requestaggregatedmeasuredata");
        var requestContent = await GetAggregatedMeasureDataContentAsync(withSyncError, cancellationToken).ConfigureAwait(false);
        request.Content = new StringContent(requestContent.Content, Encoding.UTF8, "application/json");
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        _logger.WriteLine("Sending RequestAggregatedMeasureData HTTP request with messageId: {0}", requestContent.MessageId);
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
        var orchestration = await _durableClient.WaitForOrchestationStartedAsync(orchestrationStartedAfter.ToDateTimeUtc());

        return orchestration;
    }

    internal async Task WaitForOrchestrationCompletedAsync(string orchestrationInstanceId)
    {
        await _durableClient.WaitForOrchestrationCompletedAsync(orchestrationInstanceId, TimeSpan.FromMinutes(30));
    }

    private async Task<(string MessageId, string Content)> GetRequestWholesaleSettlementContentAsync(
        bool withSyncError,
        CancellationToken cancellationToken)
    {
        var messageId = Guid.NewGuid().ToTestMessageUuid();
        var transactionId = Guid.NewGuid().ToTestMessageUuid();

        var jsonContent = await File.ReadAllTextAsync("Messages/json/RequestWholesaleSettlement.json", cancellationToken)
            .ConfigureAwait(false);

        if (withSyncError is true)
        {
            jsonContent = await File.ReadAllTextAsync("Messages/json/RequestWholesaleSettlementWithSyncError.json", cancellationToken)
                .ConfigureAwait(false);
        }

        jsonContent = jsonContent.Replace("{MessageId}", messageId, StringComparison.InvariantCulture);
        jsonContent = jsonContent.Replace("{TransactionId}", transactionId, StringComparison.InvariantCulture);

        _logger.WriteLine(
            "Creating RequestWholesaleSettlement message with MessageId={0}, TransactionId={1}",
            messageId,
            transactionId);

        return (messageId, jsonContent);
    }

    private async Task<(string MessageId, string Content)> GetMeteredDataForMeteringPointContentAsync(
        MeteringPointId meteringPointId,
        DocumentFormat documentFormat,
        CancellationToken cancellationToken)
    {
        var messageId = Guid.NewGuid().ToTestMessageUuid();
        var transactionId = Guid.NewGuid().ToTestMessageUuid();

        var filePath = documentFormat switch
        {
            var df when df == DocumentFormat.Json => "Messages/json/rsm-012-bundle-json-96points-2150transactions.json",
            var df when df == DocumentFormat.Xml => "Messages/xml/rsm-012-bundle-xml-96points-2750transactions.xml",
            _ => throw new ArgumentOutOfRangeException(nameof(documentFormat), documentFormat, "Unhandled document format"),
        };

        var content = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);

        content = content.Replace("{MessageId}", messageId, StringComparison.InvariantCulture);
        content = content.Replace("{TransactionId}", transactionId, StringComparison.InvariantCulture);
        content = content.Replace("{MeteringPointId}", meteringPointId.Value, StringComparison.InvariantCulture);

        _logger.WriteLine(
            "Creating ForwardMeteredData message with MessageId={0}, TransactionId={1}, MeteringPointId={2}, DocumentFormat={3}",
            messageId,
            transactionId,
            meteringPointId.Value,
            documentFormat.Name);

        return (messageId, content);
    }

    private async Task<(string MessageId, string Content)> GetRequestMeasurementsContentAsync(DocumentFormat documentFormat)
    {
        var messageId = Guid.NewGuid().ToTestMessageUuid();
        var transactionId = Guid.NewGuid().ToTestMessageUuid();

        var filePath = documentFormat switch
        {
            var df when df == DocumentFormat.Json => "Messages/json/RequestValidatedMeasurements.json",
            _ => throw new ArgumentOutOfRangeException(nameof(documentFormat), documentFormat, "Unhandled document format"),
        };

        var content = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);

        content = content.Replace("{MessageId}", messageId, StringComparison.InvariantCulture);
        content = content.Replace("{TransactionId}", transactionId, StringComparison.InvariantCulture);

        _logger.WriteLine(
            "Creating ForwardMeteredData message with MessageId={0}, TransactionId={1}, DocumentFormat={2}",
            messageId,
            transactionId,
            documentFormat.Name);

        return (messageId, content);
    }

    private async Task<(string MessageId, string Content)> GetAggregatedMeasureDataContentAsync(bool withSyncError, CancellationToken cancellationToken)
    {
        var messageId = Guid.NewGuid().ToTestMessageUuid();
        var transactionId = Guid.NewGuid().ToTestMessageUuid();

        var jsonContent = await File.ReadAllTextAsync("Messages/json/RequestAggregatedMeasureData.json", cancellationToken)
            .ConfigureAwait(false);

        if (withSyncError is true)
        {
            jsonContent = await File.ReadAllTextAsync("Messages/json/RequestAggregatedMeasureDataWithSyncError.json", cancellationToken)
                .ConfigureAwait(false);
        }

        jsonContent = jsonContent.Replace("{MessageId}", messageId, StringComparison.InvariantCulture);
        jsonContent = jsonContent.Replace("{TransactionId}", transactionId, StringComparison.InvariantCulture);

        _logger.WriteLine(
            "Creating RequestAggregatedMeasureData message with MessageId={0}, TransactionId={1}",
            messageId,
            transactionId);

        return (messageId, jsonContent);
    }

    private string GetMessageId(HttpResponseMessage peekResponse)
    {
        return peekResponse.Headers.GetValues("MessageId").First();
    }

    private async Task<HttpResponseMessage> PeekAsync(DocumentFormat? documentFormat = null, MessageCategory? messageCategory = null)
    {
        var b2bClient = await _b2BHttpClient;

        var messageCategoryString = messageCategory switch
        {
            null => MessageCategory.Aggregations.Name,
            var mc when mc == MessageCategory.None => throw new InvalidOperationException("Message category must be specified."),
            var mc => mc.Name,
        };

        using var request = new HttpRequestMessage(HttpMethod.Get, $"v1.0/cim/{messageCategoryString.ToLower()}");
        var contentType = documentFormat == null || DocumentFormat.Json == documentFormat ? "application/json" : "application/xml";
        request.Content = new StringContent(string.Empty, Encoding.UTF8, contentType);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        var peekResponse = await b2bClient.SendAsync(request).ConfigureAwait(false);
        await peekResponse.EnsureSuccessStatusCodeWithLogAsync(_logger);
        return peekResponse;
    }

    private async Task<HttpResponseMessage> DequeueAsync(string messageId)
    {
        var b2bClient = await _b2BHttpClient;
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"v1.0/cim/dequeue/{messageId}");
        var dequeueResponse = await b2bClient.SendAsync(request).ConfigureAwait(false);
        await dequeueResponse.EnsureSuccessStatusCodeWithLogAsync(_logger);

        return dequeueResponse;
    }
}
