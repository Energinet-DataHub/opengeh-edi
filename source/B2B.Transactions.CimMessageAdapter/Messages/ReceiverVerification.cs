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
using B2B.CimMessageAdapter.Errors;

namespace B2B.CimMessageAdapter.Messages
{
    public static class ReceiverVerification
    {
        private const string MeteringPointAdministratorRole = "DDZ";
        private const string GlnOfDataHub = "5790001330552";

        public static Task<Result> VerifyAsync(string receiverId, string role)
        {
            if (receiverId == null) throw new ArgumentNullException(nameof(receiverId));
            if (role == null) throw new ArgumentNullException(nameof(role));

            if (IsMeteringPointAdministrator(role) == false)
            {
                return Task.FromResult(Result.Failure(new InvalidReceiverRole()));
            }

            if (ReceiverIsKnown(receiverId) == false)
            {
                return Task.FromResult(Result.Failure(new UnknownReceiver(receiverId)));
            }

            return Task.FromResult(Result.Succeeded());
        }

        private static bool IsMeteringPointAdministrator(string role)
        {
            return role.Equals(MeteringPointAdministratorRole, StringComparison.OrdinalIgnoreCase);
        }

        private static bool ReceiverIsKnown(string receiverId)
        {
            return receiverId.Equals(GlnOfDataHub, StringComparison.OrdinalIgnoreCase);
        }
    }
}
