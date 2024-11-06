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
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Security;
using System.Text;
using System.Xml;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.SubsystemTests.Drivers.Ebix;

internal sealed class EbixDriver : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly marketMessagingB2BServiceV01PortTypeClient _ebixServiceClient;
    private readonly X509Certificate2? _certificate;
    private readonly HttpClient _unauthorizedHttpClient;
    private readonly HttpClient _httpClientWithCertificate;
    private readonly HttpClientHandler _certificateHttpClientHandler;

    public EbixDriver(Uri dataHubUrlEbixUrl, EbixCredentials ebixCredentials, ITestOutputHelper output)
    {
        _output = output;
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

        _certificate = _ebixServiceClient.ClientCredentials.ClientCertificate.Certificate = new X509Certificate2(
            $"./Drivers/Ebix/{ebixCredentials.CertificateName}",
            ebixCredentials.CertificatePassword);

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

    public async Task<peekMessageResponse?> PeekMessageAsync()
    {
        if (_ebixServiceClient.State != CommunicationState.Opened)
            _ebixServiceClient.Open();

        using var operationScope = new OperationContextScope(_ebixServiceClient.InnerChannel);

        var stopWatch = Stopwatch.StartNew();
        var timeBeforeTimeout = TimeSpan.FromSeconds(10);
        do
        {
            var peekResult = await _ebixServiceClient.peekMessageAsync().ConfigureAwait(false);
            if (peekResult?.MessageContainer?.Payload is not null)
                return peekResult;

            await Task.Delay(500).ConfigureAwait(false);
        }
        while (stopWatch.ElapsedMilliseconds < timeBeforeTimeout.TotalMilliseconds);

        throw new TimeoutException("Unable to retrieve peek result within time limit");
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
            if (e is MessageSecurityException messageSecurityException &&
                messageSecurityException.Message.Contains("The HTTP request is unauthorized"))
            {
                // This is a "known" exception, no need to write it to the test output as if it was an unexpected exception.
                _output.WriteLine("Dequeue ebIX request failed with unauthorized exception.");
            }
            else
            {
                _output.WriteLine("Encountered unknown CommunicationException while dequeuing. The exception was:\n{0}", e);
            }

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

    public async Task<string> SendMeteredDataForMeasurementPointAsync(CancellationToken cancellationToken)
    {
        if (_ebixServiceClient.State != CommunicationState.Opened)
            _ebixServiceClient.Open();

        using var operationScope = new OperationContextScope(_ebixServiceClient.InnerChannel);

        var messageId = Guid.NewGuid().ToString("N");
        var requestContent = await GetMeteredDataForMeasurementPointRequestContentAsync(messageId, cancellationToken).ConfigureAwait(false);
        var requestXml = new XmlDocument();
        requestXml.LoadXml(requestContent);
        var message = new MessageContainer_Type
        {
            MessageReference = messageId,
            DocumentType = "MeteredDataTimeSeries",
            MessageType = MessageType_Type.XML,
            Payload = requestXml.DocumentElement,
        };
        var response = await _ebixServiceClient.sendMessageAsync(message);

        return response.MessageId;
    }

    public async Task<string> SendMeteredDataForMeasurementPointInEbixWithAlreadyUsedMessageIdAsync(CancellationToken cancellationToken)
    {
        if (_ebixServiceClient.State != CommunicationState.Opened)
            _ebixServiceClient.Open();

        using var operationScope = new OperationContextScope(_ebixServiceClient.InnerChannel);

        var existingMessageId = "fe8eaac060c8418fae510402c6c60376";
        var requestContent = await GetMeteredDataForMeasurementPointRequestContentAsync(existingMessageId, cancellationToken).ConfigureAwait(false);
        var requestXml = new XmlDocument();
        requestXml.LoadXml(requestContent);
        var message = new MessageContainer_Type
        {
            MessageReference = existingMessageId,
            DocumentType = "MeteredDataTimeSeries",
            MessageType = MessageType_Type.XML,
            Payload = requestXml.DocumentElement,
        };

        try
        {
            var response = await _ebixServiceClient.sendMessageAsync(message);
            return response.MessageId;
        }
        catch (FaultException e)
        {
            return e.Message;
        }
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
        if (node == null)
        {
            nsmgr = new XmlNamespaceManager(new NameTable());
            nsmgr.AddNamespace("ns0", "un:unece:260:data:EEM-DK_NotifyAggregatedWholesaleServices:v3");
            query = "/ns0:HeaderEnergyDocument/ns0:Identification";
            node = response.MessageContainer.Payload.SelectSingleNode(query, nsmgr);
        }

        if (node == null)
        {
            nsmgr = new XmlNamespaceManager(new NameTable());
            nsmgr.AddNamespace("ns0", "un:unece:260:data:EEM-DK_RejectAggregatedBillingInformation:v3");
            query = "/ns0:HeaderEnergyDocument/ns0:Identification";
            node = response.MessageContainer.Payload.SelectSingleNode(query, nsmgr);
        }

        if (node == null)
        {
            nsmgr = new XmlNamespaceManager(new NameTable());
            nsmgr.AddNamespace("ns0", "un:unece:260:data:EEM-DK_RejectRequestMeteredDataAggregated:v3");
            query = "/ns0:HeaderEnergyDocument/ns0:Identification";
            node = response.MessageContainer.Payload.SelectSingleNode(query, nsmgr);
        }

        return node!.InnerText;
    }

    private async Task<peekMessageResponse?> PeekAsync()
    {
        return await _ebixServiceClient.peekMessageAsync().ConfigureAwait(false);
    }

    private async Task<string> GetMeteredDataForMeasurementPointRequestContentAsync(string messageId, CancellationToken cancellationToken)
    {
        var content = await File.ReadAllTextAsync("Messages/ebix/MeteredDataForMeasurementPoint.xml", cancellationToken)
            .ConfigureAwait(false);

        content = content.Replace("{MessageId}", messageId, StringComparison.InvariantCulture);
        content = content.Replace("{TransactionId}", Guid.NewGuid().ToString("N"), StringComparison.InvariantCulture);

        return content;
    }
}
