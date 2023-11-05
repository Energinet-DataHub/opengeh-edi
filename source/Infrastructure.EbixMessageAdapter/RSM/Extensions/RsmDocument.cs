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
using System.Collections.ObjectModel;
using Energinet.DataHub.EDI.Infrastructure.EbixMessageAdapter.Classes;
using Energinet.DataHub.EDI.Infrastructure.EbixMessageAdapter.Interfaces;
using Energinet.DataHub.EDI.Infrastructure.EbixMessageAdapter.Utilities;

namespace Energinet.DataHub.EDI.Infrastructure.EbixMessageAdapter.RSM.Extensions
{
    public abstract class RsmDocument : IRsmDocument
    {
        /// <summary>
        /// Get number of payload items
        /// </summary>
        public abstract int PayloadCount { get; }

        public static void AddRsmInformation(DataHubDto dto, dynamic payload)
        {
            ArgumentNullException.ThrowIfNull(dto, nameof(dto));

            dto.RespPayloadId = payload.Identification;
            if (payload.GetType().GetProperty("OriginalBusinessDocument") != null)
                dto.ReqPayloadId = payload.OriginalBusinessDocument;
        }

        /// <summary>
        /// Returns unique message id of document. Specified by sender, either DataHub or us (we always use RowKey)
        /// </summary>
        /// <returns>Message id</returns>
        public abstract string GetMessageId();

        /// <summary>
        /// Returns message id of referenced request document. Is always null for requests.
        /// </summary>
        /// <returns>Message id of referenced request</returns>
        public abstract string GetReferenceMessageId(int payloadIndex);

        /// <summary>
        /// Gets the response code.
        /// </summary>
        /// <returns>Response code</returns>
        public abstract string GetResponseCode(int payloadIndex);

        /// <summary>
        /// Gets the specified payload
        /// </summary>
        /// <returns>Payload document</returns>
        public abstract object GetPayload(int payloadIndex);

        /// <summary>
        /// Gets the specified payload as an xml document
        /// </summary>
        /// <returns>Payload document</returns>
        public System.Xml.XmlDocument GetPayloadXml(int payloadIndex)
        {
            return RSMUtilities.ConvertToXmlDocument(GetPayload(payloadIndex));
        }

        /// <summary>
        /// Adds payload to document
        /// </summary>
        /// <param name="payload">Payload to add</param>
        public abstract void AddPayload(object payload);

        /// <summary>
        /// Set payloads of the document. Any existing payloads are overriden.
        /// </summary>
        /// <param name="payloads">List of payloads</param>
        public abstract void SetPayloads(Collection<object> payloads);

        /// <summary>
        /// Returns unique message id of payload. Specified by sender, either DataHub or us (we always use RowKey)
        /// </summary>
        /// <returns>Message id of specified payload</returns>
        public abstract string GetPayloadMessageId(int payloadIndex);

        /// <summary>
        /// Returns document type of document.
        /// </summary>
        /// <returns>Returns document type of document</returns>
        public abstract string GetDocumentType();

        /// <summary>
        /// Gets the creation datetime.
        /// </summary>
        /// <returns>Gets the creation datetime</returns>
        public abstract DateTime GetCreation();

        /// <summary>
        /// Gets the business reason code.
        /// </summary>
        /// <returns>Gets the business reason code</returns>
        public abstract string GetBusinessReasonCode();

        /// <summary>
        /// Gets the RSM number.
        /// </summary>
        /// <returns>Gets the RSM number</returns>
        public abstract string GetRsmNumber();

        public virtual string GetOriginalBusinessDocument(int payloadIndex)
        {
            return string.Empty;
        }

        public virtual string GetMeteringPointId(int payloadIndex)
        {
            return string.Empty;
        }

        public virtual string GetServiceRequest(int payloadIndex)
        {
            return string.Empty;
        }

        public virtual DateTime? GetOccurrence(int payloadIndex)
        {
            return null;
        }
    }
}
