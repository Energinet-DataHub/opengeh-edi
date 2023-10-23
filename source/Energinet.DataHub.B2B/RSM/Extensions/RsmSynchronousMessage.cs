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
using System.IO;
using System.Xml.Serialization;

namespace Energinet.DataHub.B2B.RSM.Extensions
{
    public abstract class RsmSynchronousMessage : RsmDocument
    {
        public DateTime Creation { get; set; }

        public string OriginalBusinessDocument { get; set; } = string.Empty;

        public string Identification { get; set; } = string.Empty;

        public override int PayloadCount
        {
            get { return 1; }
        }

        public override string GetMessageId()
        {
            return Identification;
        }

        public override string GetReferenceMessageId(int payloadIndex)
        {
            return OriginalBusinessDocument;
        }

        public override string ToString()
        {
            var xmlSerializer = new XmlSerializer(GetType());

            using (var textWriter = new StringWriter())
            {
                xmlSerializer.Serialize(textWriter, this);
                return textWriter.ToString();
            }
        }

        public override DateTime GetCreation()
        {
            return Creation;
        }

        public override object GetPayload(int payloadIndex)
        {
            return this;
        }

        public override string GetPayloadMessageId(int payloadIndex)
        {
            return Identification;
        }

        public override void AddPayload(object payload)
        {
            throw new NotSupportedException("No payloads in this message");
        }

        public override void SetPayloads(System.Collections.Generic.List<object> payloads)
        {
            throw new NotSupportedException("No payloads in this message");
        }

        public override string GetResponseCode(int payloadIndex)
        {
            throw new NotImplementedException();
        }

        public override string GetDocumentType()
        {
            throw new NotImplementedException();
        }

        public override string GetBusinessReasonCode()
        {
            throw new NotImplementedException();
        }

        public override string GetRsmNumber()
        {
            throw new NotImplementedException();
        }
    }
}
