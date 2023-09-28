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
using System.Threading.Tasks;
using Energinet.DataHub.EDI.Domain.Actors;
using Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.ValidationErrors;

namespace Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages
{
    /// <summary>
    /// Responsible for verifying that the Receiver is a Master Data Responsible Receiver and Datahub
    /// </summary>
    public class MasterDataReceiverResponsibleVerification : IReceiverValidator
    {
        private const string MasterDataResponsibleRole = "DDZ";
        private const string GlnOfDataHub = "5790001330552";

        public Task<Result> VerifyAsync(ActorNumber receiverNumber, MarketRole receiverRole)
        {
            if (receiverNumber == null) throw new ArgumentNullException(nameof(receiverNumber));
            if (receiverRole == null) throw new ArgumentNullException(nameof(receiverRole));

            if (IsMasterDataResponsible(receiverRole.Code) == false)
            {
                return Task.FromResult(Result.Failure(new InvalidReceiverRole()));
            }

            if (ReceiverIsDataHub(receiverNumber.Value) == false)
            {
                return Task.FromResult(Result.Failure(new InvalidReceiverId(receiverNumber.Value)));
            }

            return Task.FromResult(Result.Succeeded());
        }

        private static bool IsMasterDataResponsible(string role)
        {
            return role.Equals(MasterDataResponsibleRole, StringComparison.OrdinalIgnoreCase);
        }

        private static bool ReceiverIsDataHub(string receiverId)
        {
            return receiverId.Equals(GlnOfDataHub, StringComparison.OrdinalIgnoreCase);
        }
    }
}
