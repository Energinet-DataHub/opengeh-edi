﻿// Copyright 2020 Energinet DataHub A/S
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

using System.Collections.Generic;
using System.Linq;
using Energinet.DataHub.MarketData.Domain.SeedWork;
using NodaTime;

namespace Energinet.DataHub.MarketData.Domain.MeteringPoints.Rules.ChangeEnergySupplier
{
    public class MoveInRegisteredOnSameDateIsNotAllowedRule : IBusinessRule
    {
        private readonly IReadOnlyList<Relationship> _processes;
        private readonly Instant _effectuationDate;

        internal MoveInRegisteredOnSameDateIsNotAllowedRule(IReadOnlyList<Relationship> processes, Instant effectuationDate)
        {
            _processes = processes;
            _effectuationDate = effectuationDate;
        }

        public bool IsBroken => HasMoveInRegisteredOnDate();

        public string Message => "A move in process (BRS-009) is registered on same date.";

        private bool HasMoveInRegisteredOnDate()
        {
            return _processes.Any(p => p.Type == RelationshipType.Customer1 &&
                                       p.EffectuationDate.ToDateTimeUtc().Date
                                           .Equals(_effectuationDate.ToDateTimeUtc().Date));
        }
    }
}
