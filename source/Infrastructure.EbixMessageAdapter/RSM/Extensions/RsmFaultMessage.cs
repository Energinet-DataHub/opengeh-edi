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

namespace Energinet.DataHub.EDI.Infrastructure.EbixMessageAdapter.RSM.Extensions
{
    /// <summary>
    /// Class containing fault information from DataHub after synchronous validation
    /// </summary>
    [Serializable]
    public class RsmFaultMessage : RsmSynchronousMessage
    {
        public string ReasonText { get; set; } = string.Empty; // ErrorMessage

        public string ResponseReasonType { get; set; } = string.Empty; // Errordetails

        /// <summary>
        /// Is it a relaying error
        /// </summary>
        public bool IsRelayingError { get; set; }

        public override string GetDocumentType()
        {
            return "FaultMessage";
        }

        public override string GetBusinessReasonCode()
        {
            return string.Empty;
        }

        public override string GetResponseCode(int payloadIndex)
        {
            return ReasonText.Length <= 25 ? ReasonText : ReasonText.Substring(0, 24);
        }

        public override string GetRsmNumber()
        {
            return string.Empty;
        }
    }
}
