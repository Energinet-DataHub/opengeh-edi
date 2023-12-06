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
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix;

internal sealed class EbixDriver : IDisposable
{
    private readonly marketMessagingB2BServiceV01PortTypeClient _ebixServiceClient;
    private readonly X509Certificate2? _certificate;

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
    }

    public async Task<peekMessageResponse?> PeekMessageWithTimeoutAsync()
    {
        if (_ebixServiceClient.State != CommunicationState.Opened)
            _ebixServiceClient.Open();

        using var operationScope = new OperationContextScope(_ebixServiceClient.InnerChannel);

        // Add a HTTP Header to an outgoing request
        var requestMessage = new HttpRequestMessageProperty();
        requestMessage.Headers.Add(HttpRequestHeader.ContentType, "text/xml");

        OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = requestMessage;

        var stopWatch = Stopwatch.StartNew();
        var timeBeforeTimeout = new TimeSpan(0, 1, 0);
        Exception? lastException = null;
        while (stopWatch.ElapsedMilliseconds < timeBeforeTimeout.TotalMilliseconds)
        {
            try
            {
                return await _ebixServiceClient.peekMessageAsync().ConfigureAwait(false);
            }
            catch (CommunicationException e)
            {
                Console.WriteLine("Encountered CommunicationException while peeking. This is probably because the message hasn't been handled yet, so we're trying again in 500ms. The exception was:");
                Console.WriteLine(e);
                lastException = e;
            }

            await Task.Delay(500).ConfigureAwait(false);
        }

        throw new TimeoutException("Unable to retrieve peek result within time limit", lastException);
    }

    public void Dispose()
    {
        if (_ebixServiceClient.State != CommunicationState.Closed)
            _ebixServiceClient.Close();

        _certificate?.Dispose();
    }
}
