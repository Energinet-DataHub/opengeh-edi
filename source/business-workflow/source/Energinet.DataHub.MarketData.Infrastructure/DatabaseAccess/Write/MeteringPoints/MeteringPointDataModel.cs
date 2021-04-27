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
using System.Collections.Generic;

namespace Energinet.DataHub.MarketData.Infrastructure.DatabaseAccess.Write.MeteringPoints
{
    public class MeteringPointDataModel
    {
#pragma warning disable 8618 //Empty constructor needed to satisfy EntityFramework
        public MeteringPointDataModel()
#pragma warning restore 8618
        { }

        public MeteringPointDataModel(Guid id, string gsrnNumber, int type, bool productionObligated, int physicalState, ICollection<RelationshipDataModel> relationshipDataModels, int version)
        {
            Id = id;
            GsrnNumber = gsrnNumber;
            Type = type;
            ProductionObligated = productionObligated;
            PhysicalState = physicalState;
            RelationshipDataModels = relationshipDataModels;
            RowVersion = version;
        }

        public Guid Id { get; set; }

        public string GsrnNumber { get; set; }

        public int Type { get; set; }

        public bool ProductionObligated { get; set; }

        public int PhysicalState { get; set; }

        public int RowVersion { get; set; }

        public ICollection<RelationshipDataModel> RelationshipDataModels { get; set; }
    }
}
