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

namespace SoapTest.RSM.Extensions
{
    /// <summary>
    /// Class containing confirmation information from DataHub after synchronous validation
    /// </summary>
    [Serializable]
    public class RsmConfirmMessage : RsmSynchronousMessage
    {
        /// <summary>
        /// Any fault action applied
        /// </summary>
        public bool FaultActionApplied { get; set; }

        /// <summary>
        /// The name of the action name
        /// </summary>
        public string FaultActionName { get; set; } = string.Empty;

        /// <summary>
        /// B2B_003 error from DataHub - the DataHub allready have received the data and do not wants it again
        /// </summary>
        public bool B2B_003 { get; set; }

        public override string GetDocumentType()
        {
            return "ConfirmMessage";
        }

        public override string GetBusinessReasonCode()
        {
            return string.Empty;
        }

        public override string GetRsmNumber()
        {
            return string.Empty;
        }

        public override string GetResponseCode(int payloadIndex)
        {
            return string.Empty;
        }
    }
}
