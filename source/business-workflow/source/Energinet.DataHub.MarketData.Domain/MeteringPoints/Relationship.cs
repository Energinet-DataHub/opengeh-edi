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
using Energinet.DataHub.MarketData.Domain.SeedWork;
using NodaTime;

namespace Energinet.DataHub.MarketData.Domain.MeteringPoints
{
    public class Relationship : Entity
    {
        public Relationship(MarketParticipantMrid marketParticipantMrid, RelationshipType type, Instant effectuationDate)
        {
            MarketParticipantMrid = marketParticipantMrid ?? throw new ArgumentNullException(nameof(marketParticipantMrid));
            Type = type ?? throw new ArgumentNullException(nameof(type));
            EffectuationDate = effectuationDate;
            State = RelationshipState.Pending;
        }

        private Relationship(int id, MarketParticipantMrid marketParticipantMrid, RelationshipType type, RelationshipState state, Instant effectuationDate)
        {
            Id = id;
            MarketParticipantMrid = marketParticipantMrid ?? throw new ArgumentNullException(nameof(marketParticipantMrid));
            Type = type ?? throw new ArgumentNullException(nameof(type));
            EffectuationDate = effectuationDate;
            State = state;
        }

        public MarketParticipantMrid MarketParticipantMrid { get; }

        public RelationshipType Type { get; }

        public Instant EffectuationDate { get; }

        public RelationshipState State { get; private set; }

        public static Relationship CreateFrom(RelationshipSnapshot snapshot)
        {
            if (snapshot is null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            return new Relationship(
                snapshot.Id,
                new MarketParticipantMrid(snapshot.MarketParticipantMrid),
                RelationshipType.FromValue<RelationshipType>(snapshot.Type),
                RelationshipState.FromValue<RelationshipState>(snapshot.State),
                snapshot.EffectuationDate);
        }

        public void Activate()
        {
            if (State != RelationshipState.Active)
            {
                State = RelationshipState.Active;
            }
        }

        public RelationshipSnapshot GetSnapshot()
        {
            return new RelationshipSnapshot(Id, MarketParticipantMrid.Mrid, Type.Id, EffectuationDate, State.Id);
        }
    }
}
