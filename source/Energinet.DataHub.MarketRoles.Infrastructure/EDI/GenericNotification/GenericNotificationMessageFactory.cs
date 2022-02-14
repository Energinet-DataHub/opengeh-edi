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
using Energinet.DataHub.MarketRoles.Infrastructure.EDI.Common;
using NodaTime;

namespace Energinet.DataHub.MarketRoles.Infrastructure.EDI.GenericNotification
{
    public static class GenericNotificationMessageFactory
    {
        public static GenericNotificationMessage GenericNotification(
            MarketRoleParticipant sender,
            MarketRoleParticipant receiver,
            Instant createdDateTime,
            string gsrn,
            Instant startDateAndOrTime,
            string? transactionId)
        {
            return new GenericNotificationMessage(
                DocumentName: "GenericNotification_MarketDocument",
                Id: Guid.NewGuid().ToString(),
                Type: "E44",
                ProcessType: "E65",
                BusinessSectorType: "E23",
                Sender: sender,
                Receiver: receiver,
                CreatedDateTime: createdDateTime,
                MarketActivityRecord: new MarketActivityRecord(
                    Id: Guid.NewGuid().ToString(),
                    MarketEvaluationPoint: gsrn,
                    StartDateAndOrTime: startDateAndOrTime,
                    // TODO: This should be replaced with the actual transactionID when this is moved to MarketRoles. Currently the message has not actually been sent by the market actor and thus they don't have an actual transaction ID.
                    OriginalTransaction: transactionId ?? Guid.NewGuid().ToString()));
        }
    }
}
