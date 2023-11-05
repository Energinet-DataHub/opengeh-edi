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

using System;
using System.Configuration;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Xml;
using SoapTest.RSM.Extensions;

namespace Kamstrup.DataHub.Integration.DataHub
{
    public class DataHubBroker
    {
#nullable enable
        private DataHubService.marketMessagingB2BServiceV01PortTypeClient? _dataHubServiceClient = null;
#nullable disable

        protected DataHubService.marketMessagingB2BServiceV01PortTypeClient DataHubServiceClient
        {
            get
            {
                if (_dataHubServiceClient == null)
                {
                    var stopWatch = new Stopwatch();
                    stopWatch.Restart();

                    //var certificateName = "Insert CertificateName";
                    //var certificateSerialNumber = "Insert CertificateSerialNumber";

                    var dataHubUrl = ConfigurationManager.AppSettings["TestUrl"];

                    // Create a binding using Transport and a certificate.
                    var binding = new BasicHttpBinding();
                    binding.Security.Mode = BasicHttpSecurityMode.Transport;
                    //binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Certificate;
                    binding.MaxReceivedMessageSize = 52428800; // 50 MB

                    // Create an EndPointAddress.
                    var endpoint = new EndpointAddress(dataHubUrl);

                    // Create the client.
                    _dataHubServiceClient = new DataHubService.marketMessagingB2BServiceV01PortTypeClient(binding, endpoint);
                    // Set the certificate for the client.
                    //if (!string.IsNullOrEmpty(certificateName))
                    //{
                    //    _dataHubServiceClient.ClientCredentials.ClientCertificate.SetCertificate(StoreLocation.LocalMachine, StoreName.My, X509FindType.FindBySubjectName, certificateName);
                    //}
                    //else if (!string.IsNullOrEmpty(certificateSerialNumber))
                    //{
                    //    _dataHubServiceClient.ClientCredentials.ClientCertificate.SetCertificate(StoreLocation.LocalMachine, StoreName.My, X509FindType.FindBySerialNumber, certificateSerialNumber);
                    //}

                    stopWatch.Stop();
                }

                return _dataHubServiceClient;
            }
        }

        /// <summary>
        /// Remove message from DataHub queue
        /// </summary>
        /// <param name="msgID">Id of message to remove</param>
        public void DequeueMessage(string msgID)
        {
            DataHubServiceClient.dequeueMessage(msgID);
        }

        public void GetMessage(string msgID)
        {
            DataHubServiceClient.getMessage(msgID);
        }

#nullable enable

        /// <summary>
        /// Checks for waiting responses
        /// </summary>
        /// <returns>Id of next waiting or null if queue is empty</returns>
        public XmlDocument? PeekMessage(string dataHubUrl, string token)
        {
            if (string.IsNullOrWhiteSpace(dataHubUrl))
            {
                throw new ArgumentNullException(nameof(dataHubUrl));
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentNullException(nameof(token));
            }

            // Create a binding using Transport and a certificate.
            var binding = new BasicHttpBinding();
            binding.Security.Mode = BasicHttpSecurityMode.Transport;
            //binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Certificate;
            binding.MaxReceivedMessageSize = 52428800; // 50 MB

            // Create an EndPointAddress.
            var endpoint = new EndpointAddress(dataHubUrl);

            // Create the client.
            var dataHubServiceClient = new DataHubService.marketMessagingB2BServiceV01PortTypeClient(binding, endpoint);
            dataHubServiceClient.Open();
            using (new OperationContextScope(dataHubServiceClient.InnerChannel))
            {
                // Add a HTTP Header to an outgoing request
                var requestMessage = new HttpRequestMessageProperty();
                requestMessage.Headers["Bearer"] = token;
                OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name]
                   = requestMessage;

                var message = dataHubServiceClient.peekMessage();

                if (message != null)
                {
                    var xmlDoc = new XmlDocument();
                    var node = xmlDoc.ImportNode(message.Payload, true);
                    xmlDoc.AppendChild(node);
                    return xmlDoc;
                }
            }

            return null;
        }

        public XmlDocument? GetMessage(string dataHubUrl, string token, string messageId)
        {
            if (string.IsNullOrWhiteSpace(dataHubUrl))
            {
                throw new ArgumentNullException(nameof(dataHubUrl));
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentNullException(nameof(token));
            }

            // Create a binding using Transport and a certificate.
            var binding = new BasicHttpBinding();
            binding.Security.Mode = BasicHttpSecurityMode.Transport;
            //binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Certificate;
            binding.MaxReceivedMessageSize = 52428800; // 50 MB

            // Create an EndPointAddress.
            var endpoint = new EndpointAddress(dataHubUrl);

            // Create the client.
            var dataHubServiceClient = new DataHubService.marketMessagingB2BServiceV01PortTypeClient(binding, endpoint);
            dataHubServiceClient.Open();
            using (new OperationContextScope(dataHubServiceClient.InnerChannel))
            {
                // Add a HTTP Header to an outgoing request
                var requestMessage = new HttpRequestMessageProperty();
                requestMessage.Headers["Bearer"] = token;
                OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name]
                   = requestMessage;

                var message = dataHubServiceClient.getMessage(messageId);

                if (message != null)
                {
                    var xmlDoc = new XmlDocument();
                    var node = xmlDoc.ImportNode(message.Payload, true);
                    xmlDoc.AppendChild(node);
                    return xmlDoc;
                }
            }

            return null;
        }

#nullable disable

        public RsmSynchronousMessage SendMessage(XmlDocument document, string messageId, string documentType)
        {
            try
            {
                var msgContainer = new DataHubService.MessageContainer_Type();
                msgContainer.MessageReference = messageId;
                msgContainer.DocumentType = documentType;
                msgContainer.MessageType = DataHubService.MessageType_Type.XML;
                msgContainer.Payload = document.DocumentElement;

                var stopWatch = new Stopwatch();
                stopWatch.Restart();
                DataHubServiceClient.sendMessage(msgContainer);
                stopWatch.Stop();

                return new RsmConfirmMessage();
            }
            catch (FaultException<ExceptionDetail> e)
            {
                var detail = e.Detail.ToString();
                if (detail.StartsWith("B2B-003:", StringComparison.InvariantCultureIgnoreCase))
                {
                    // The exception says, that the DataHub allready have received the data and do not wants it again :-)
                    return new RsmConfirmMessage() { B2B_003 = true };
                }
                else
                {
                    var fault = new RsmFaultMessage
                    {
                        ReasonText = e.Message,
                        ResponseReasonType = detail,
                    };

                    return fault;
                }
            }
            catch (FaultException e)
            {
                var reason = e.Reason.ToString();
                if (reason.StartsWith("B2B-003:", StringComparison.InvariantCultureIgnoreCase))
                {
                    // The exception says, that the DataHub allready have received the data and do not wants it again :-)
                    return new RsmConfirmMessage() { B2B_003 = true };
                }
                else
                {
                    var fault = new RsmFaultMessage();
                    fault.ReasonText = e.Message;
                    fault.ResponseReasonType = reason;

                    return fault;
                }
            }
        }
    }
}
