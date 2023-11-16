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

using System.Configuration;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix;

internal sealed class EbixDriver : IDisposable
{
    private readonly marketMessagingB2BServiceV01PortTypeClient _ebixServiceClient;

    public EbixDriver(Uri dataHubUrlEbixUrl)
    {
        string? certificateName = null;
        string? certificateSerialNumber = null;

        // Create a binding using Transport and a certificate.
        var binding = new BasicHttpBinding
        {
            Security = { Mode = BasicHttpSecurityMode.Transport },
            MaxReceivedMessageSize = 52428800, // 50 MB
        };

        if (!string.IsNullOrEmpty(certificateName) || !string.IsNullOrEmpty(certificateSerialNumber))
            binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Certificate;

        // Create an EndPointAddress.
        var endpoint = new EndpointAddress(dataHubUrlEbixUrl);

        _ebixServiceClient = new marketMessagingB2BServiceV01PortTypeClient(binding, endpoint);

        // Set the certificate for the client.
        if (!string.IsNullOrEmpty(certificateName))
            _ebixServiceClient.ClientCredentials.ClientCertificate.SetCertificate(StoreLocation.LocalMachine, StoreName.My, X509FindType.FindBySubjectName, certificateName);
        else if (!string.IsNullOrEmpty(certificateSerialNumber))
            _ebixServiceClient.ClientCredentials.ClientCertificate.SetCertificate(StoreLocation.LocalMachine, StoreName.My, X509FindType.FindBySerialNumber, certificateSerialNumber);
    }

    public async Task<peekMessageResponse?> PeekMessageWithTimeoutAsync(string token)
    {
        if (string.IsNullOrEmpty(token)) throw new ArgumentNullException(nameof(token));

        if (_ebixServiceClient.State != CommunicationState.Opened)
            _ebixServiceClient.Open();

        using var operationScope = new OperationContextScope(_ebixServiceClient.InnerChannel);

        // Add a HTTP Header to an outgoing request
        var requestMessage = new HttpRequestMessageProperty
        {
            Headers =
            {
                ["Authorization"] = $"bearer {token}",
                ["Content-Type"] = "text/xml",
            },
        };

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
        // if (message == null)
        //     return null;
        //
        // if (message != null)
        // {
        //     var xmlDoc = new XmlDocument();
        //     var node = xmlDoc.ImportNode(message.Payload, true);
        //     xmlDoc.AppendChild(node);
        //     return xmlDoc;
        // }
    }

    public void Dispose()
    {
        if (_ebixServiceClient.State != CommunicationState.Closed)
            _ebixServiceClient.Close();
    }
}
