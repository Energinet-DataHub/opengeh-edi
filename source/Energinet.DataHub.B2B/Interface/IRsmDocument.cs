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
using System.Collections.Generic;

namespace Energinet.DataHub.B2B.Interface
{
    /// <summary>
    /// Contains the interface of the IRsmDocument
    /// </summary>
    public interface IRsmDocument
    {
        /// <summary>
        /// Gets the number of payloads in the message
        /// </summary>
        int PayloadCount { get; }

        /// <summary>
        /// Gets the ID of the message
        /// </summary>
        /// <returns>Returns the ID of the message</returns>
        string GetMessageId();

        /// <summary>
        /// Gets the messageid from the payloadIndex
        /// </summary>
        /// <param name="payloadIndex"></param>
        /// <returns>Returns the messageid from the payloadIndex</returns>
        string GetReferenceMessageId(int payloadIndex);

        /// <summary>
        /// Gets the response code from the payloadIndex
        /// </summary>
        /// <param name="payloadIndex"></param>
        /// <returns>Returns the response code from the payloadIndex</returns>
        string GetResponseCode(int payloadIndex);

        /// <summary>
        /// Gets the payload at the specified index
        /// </summary>
        /// <param name="payloadIndex"></param>
        /// <returns>Returns the payload at the specified index</returns>
        object GetPayload(int payloadIndex);

        /// <summary>
        /// Gets the payload at the specified index
        /// </summary>
        /// <param name="payloadIndex"></param>
        /// <returns>Returns the payload formatet as XML</returns>
        System.Xml.XmlDocument GetPayloadXml(int payloadIndex);

        /// <summary>
        /// Adds the supplied payload to the Rsm document
        /// </summary>
        /// <param name="payload"></param>
        void AddPayload(object payload);

        /// <summary>
        /// Sets the payloads of the rsm document
        /// </summary>
        /// <param name="payloads"></param>
        void SetPayloads(List<object> payloads);

        /// <summary>
        /// Gets the id of the payload
        /// </summary>
        /// <param name="payloadIndex"></param>
        /// <returns>Returns the id of the payload </returns>
        string GetPayloadMessageId(int payloadIndex);

        /// <summary>
        /// Gets the documentype of the rsm document
        /// </summary>
        /// <returns>Returns the documentype of the rsm document</returns>
        string GetDocumentType();

        /// <summary>
        /// Gets the creation date of the rsm document
        /// </summary>
        /// <returns>Returns the creation date of the rsm document</returns>
        DateTime GetCreation();

        /// <summary>
        /// Gets the business reasoncode of the rsm document
        /// </summary>
        /// <returns>Returns the business reasoncode of the rsm document</returns>
        string GetBusinessReasonCode();

        /// <summary>
        /// Gets the rsm number of the rsm document
        /// </summary>
        /// <returns>Returns the rsm number of the rsm document</returns>
        string GetRsmNumber();

        /// <summary>
        /// Gets the OriginalBusinessDocument id at the specified index
        /// </summary>
        /// <param name="payloadIndex"></param>
        /// <returns>Returns the OriginalBusinessDocument id at the specified index</returns>
        string GetOriginalBusinessDocument(int payloadIndex);

        /// <summary>
        /// Gets the meteringpointid at the specified index
        /// </summary>
        /// <param name="payloadIndex"></param>
        /// <returns>Returns the meteringpointid at the specified index</returns>
        string GetMeteringPointId(int payloadIndex);

        /// <summary>
        /// Gets the service request at the specified idex
        /// </summary>
        /// <param name="payloadIndex"></param>
        /// <returns>Returns the service request at the specified idex</returns>
        string GetServiceRequest(int payloadIndex);
    }
}
