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
using Energinet.DataHub.MarketData.Infrastructure.DataPersistence;
using NodaTime;

namespace Energinet.DataHub.MarketData.Infrastructure.Outbox
{
    public class OutgoingActorMessage : IDataModel
    {
        public OutgoingActorMessage(int id, Instant occurredOn, string type, string data, string? recipient, Instant? processedDate, int state)
        {
            Id = id;
            OccurredOn = occurredOn;
            Type = type;
            Data = data;
            Recipient = recipient;
            ProcessedDate = processedDate;
            State = state;
        }

        public OutgoingActorMessage(Instant occurredOn, string type, string data, string? recipient)
        {
            OccurredOn = occurredOn;
            Type = type;
            Data = data;
            Recipient = recipient;
        }

        public OutgoingActorMessage(Instant occurredOn, string type, string data)
        {
            OccurredOn = occurredOn;
            Type = type;
            Data = data;
        }

        public string? Recipient { get; set; }

        public int Id { get; set; }

        public Instant OccurredOn { get; set; }

        public string Type { get; set; }

        public string Data { get; set; }

        public int State { get; set; }

        public Instant? ProcessedDate { get; set; }
    }
}
