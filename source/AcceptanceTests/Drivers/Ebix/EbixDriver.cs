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
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Xml;

namespace Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix;

internal sealed class EbixDriver : IDisposable
{
    private readonly marketMessagingB2BServiceV01PortTypeClient _ebixServiceClient;
    private readonly X509Certificate2? _certificate;
    private readonly HttpClient _unauthorizedHttpClient;
    private readonly HttpClient _httpClientWithCertificate;
    private readonly HttpClientHandler _certificateHttpClientHandler;

    public EbixDriver(Uri dataHubUrlEbixUrl, string ebixCertificatePassword)
    {
        // Create a binding using Transport and a certificate.
        var binding = new BasicHttpBinding
        {
            Security =
            {
                Mode = BasicHttpSecurityMode.Transport,
                Transport = new HttpTransportSecurity
                {
                    ClientCredentialType = HttpClientCredentialType.Certificate,
                },
            },
            MaxReceivedMessageSize = 52428800, // 50 MB
        };

        // Create an EndPointAddress.
        var endpoint = new EndpointAddress(dataHubUrlEbixUrl);

        _ebixServiceClient = new marketMessagingB2BServiceV01PortTypeClient(binding, endpoint);

        var certificateRaw = File.ReadAllBytes("./Drivers/Ebix/DH3-test-mosaik-1-private-and-public.pfx");
        _certificate = new X509Certificate2(certificateRaw, ebixCertificatePassword);
        _ebixServiceClient.ClientCredentials.ClientCertificate.Certificate = _certificate;

        _unauthorizedHttpClient = new HttpClient
        {
            BaseAddress = dataHubUrlEbixUrl,
        };

        _certificateHttpClientHandler = new HttpClientHandler
        {
            ClientCertificates = { _certificate },
            CheckCertificateRevocationList = true,
        };

        _httpClientWithCertificate = new HttpClient(_certificateHttpClientHandler)
        {
            BaseAddress = dataHubUrlEbixUrl,
        };
    }

    public async Task EmptyQueueAsync()
    {
        var peekResponse = await PeekAsync()
            .ConfigureAwait(false);

        if (peekResponse?.MessageContainer?.Payload is not null)
        {
            await DequeueMessageAsync(GetMessageId(peekResponse)).ConfigureAwait(false);
            await EmptyQueueAsync().ConfigureAwait(false);
        }
    }

    public async Task<peekMessageResponse?> PeekMessageAsync(int? timeoutInSeconds = 600)
    {
        if (_ebixServiceClient.State != CommunicationState.Opened)
            _ebixServiceClient.Open();

        using var operationScope = new OperationContextScope(_ebixServiceClient.InnerChannel);

        var stopWatch = Stopwatch.StartNew();
        var timeBeforeTimeout = timeoutInSeconds != null ? new TimeSpan(0, 0, timeoutInSeconds.Value) : TimeSpan.Zero;
        Exception? lastException = null;
        do
        {
            var peekResult = await _ebixServiceClient.peekMessageAsync().ConfigureAwait(false);
            if (peekResult?.MessageContainer?.Payload is not null)
                return peekResult;

            await Task.Delay(500).ConfigureAwait(false);
        }
        while (stopWatch.ElapsedMilliseconds < timeBeforeTimeout.TotalMilliseconds);

        throw new TimeoutException("Unable to retrieve peek result within time limit", lastException);
    }

    public async Task DequeueMessageAsync(string messageId)
    {
        if (_ebixServiceClient.State != CommunicationState.Opened)
            _ebixServiceClient.Open();

        using var operationScope = new OperationContextScope(_ebixServiceClient.InnerChannel);

        // Add a HTTP Header to an outgoing request
        var requestMessage = new HttpRequestMessageProperty();

        OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = requestMessage;

        try
        {
            await _ebixServiceClient.dequeueMessageAsync(messageId).ConfigureAwait(false);
        }
        catch (CommunicationException e)
        {
            Console.WriteLine(
                "Encountered CommunicationException while dequeuing. The exception was:");
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<HttpResponseMessage> DequeueWithoutRequestBodyAsync()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("?soapAction=dequeueMessage", UriKind.Relative));

        var emptyRequestBody = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>());

        request.Content = emptyRequestBody;

        return await _httpClientWithCertificate.SendAsync(request).ConfigureAwait(false);
    }

    public async Task<HttpResponseMessage> PeekMessageWithoutCertificateAsync()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("?soapAction=peekMessage", UriKind.Relative));
        request.Content = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>());

        return await _unauthorizedHttpClient.SendAsync(request).ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (_ebixServiceClient.State != CommunicationState.Closed)
            _ebixServiceClient.Close();

        _certificate?.Dispose();
        _unauthorizedHttpClient.Dispose();
        _certificateHttpClientHandler.Dispose();
        _httpClientWithCertificate.Dispose();
    }

    public async Task<HttpResponseMessage> DequeueMessageWithoutCertificateAsync()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("?soapAction=dequeueMessage", UriKind.Relative));
        request.Content = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>());

        return await _unauthorizedHttpClient.SendAsync(request).ConfigureAwait(false);
    }

    private static string GetMessageId(peekMessageResponse response)
    {
        var nsmgr = new XmlNamespaceManager(new NameTable());
        nsmgr.AddNamespace("ns0", "un:unece:260:data:EEM-DK_AggregatedMeteredDataTimeSeries:v3");
        var query = "/ns0:HeaderEnergyDocument/ns0:Identification";
        var node = response.MessageContainer.Payload.SelectSingleNode(query, nsmgr);
        return node!.InnerText;
    }

    private async Task<peekMessageResponse?> PeekAsync()
    {
        return await _ebixServiceClient.peekMessageAsync().ConfigureAwait(false);
    }
}
