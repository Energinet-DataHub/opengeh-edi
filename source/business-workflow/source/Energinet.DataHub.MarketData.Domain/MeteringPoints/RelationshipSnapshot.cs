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
using NodaTime;

namespace Energinet.DataHub.MarketData.Domain.MeteringPoints
{
    public class RelationshipSnapshot
    {
        public RelationshipSnapshot(Guid id, string marketParticipantMrId, int type, Instant effectuationDate, int state)
        {
            Id = id;
            MarketParticipantMrId = marketParticipantMrId;
            Type = type;
            EffectuationDate = effectuationDate;
            State = state;
        }

        public Guid Id { get; set; }

        public string MarketParticipantMrId { get; set; }

        public int Type { get; set; }

        public Instant EffectuationDate { get; set; }

        public int State { get; set; }
    }
}
