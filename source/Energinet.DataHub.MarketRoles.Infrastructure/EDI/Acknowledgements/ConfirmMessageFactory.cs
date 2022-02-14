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

namespace Energinet.DataHub.MarketRoles.Infrastructure.EDI.Acknowledgements
{
    public static class ConfirmMessageFactory
    {
        public static ConfirmMessage CreateMeteringPoint(
            MarketRoleParticipant sender,
            MarketRoleParticipant receiver,
            Instant createdDateTime,
            MarketActivityRecord marketActivityRecord)
        {
            return Defaults()
                with
                {
                    ProcessType = "E02",
                    Sender = sender,
                    Receiver = receiver,
                    CreatedDateTime = createdDateTime,
                    MarketActivityRecord = marketActivityRecord,
                };
        }

        public static ConfirmMessage ConnectMeteringPoint(
            MarketRoleParticipant sender,
            MarketRoleParticipant receiver,
            Instant createdDateTime,
            MarketActivityRecord marketActivityRecord)
        {
            return Defaults()
                with
                {
                    ProcessType = "D15",
                    Sender = sender,
                    Receiver = receiver,
                    CreatedDateTime = createdDateTime,
                    MarketActivityRecord = marketActivityRecord,
                };
        }

        public static ConfirmMessage DisconnectMeteringPoint(
            MarketRoleParticipant sender,
            MarketRoleParticipant receiver,
            Instant createdDateTime,
            MarketActivityRecord marketActivityRecord)
        {
            return Defaults()
                with
                {
                    ProcessType = "E79",
                    Sender = sender,
                    Receiver = receiver,
                    CreatedDateTime = createdDateTime,
                    MarketActivityRecord = marketActivityRecord,
                };
        }

        public static ConfirmMessage ReconnectMeteringPoint(
            MarketRoleParticipant sender,
            MarketRoleParticipant receiver,
            Instant createdDateTime,
            MarketActivityRecord marketActivityRecord)
        {
            return Defaults()
                with
                {
                    ProcessType = "E79",
                    Sender = sender,
                    Receiver = receiver,
                    CreatedDateTime = createdDateTime,
                    MarketActivityRecord = marketActivityRecord,
                };
        }

        public static ConfirmMessage RequestCloseDown(
            MarketRoleParticipant sender,
            MarketRoleParticipant receiver,
            Instant createdDateTime,
            MarketActivityRecord marketActivityRecord)
        {
            return Defaults()
                with
                {
                    ProcessType = "D14",
                    Sender = sender,
                    Receiver = receiver,
                    CreatedDateTime = createdDateTime,
                    MarketActivityRecord = marketActivityRecord,
                };
        }

        private static ConfirmMessage Defaults()
        {
            return new ConfirmMessage(
                DocumentName: "ConfirmRequestChangeAccountingPointCharacteristics_MarketDocument",
                Id: Guid.NewGuid().ToString(),
                Type: "E59", // Changes with the document type. eg E59 for ConfirmRequestChangeAccountingPointCharacteristics_MarketDocument
                ProcessType: string.Empty, // Changes with BRS, eg D15 for connect
                BusinessSectorType: "23", // Electricity
                Sender: new MarketRoleParticipant(
                    Id: string.Empty,
                    CodingScheme: string.Empty,
                    Role: string.Empty),
                Receiver: new MarketRoleParticipant(
                    Id: string.Empty,
                    CodingScheme: string.Empty,
                    Role: string.Empty),
                CreatedDateTime: Instant.MinValue,
                ReasonCode: "A01", // Confirm
                MarketActivityRecord: new MarketActivityRecord(
                    Id: string.Empty,
                    MarketEvaluationPoint: string.Empty,
                    OriginalTransaction: string.Empty));
        }
    }
}
