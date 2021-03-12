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

using System.Collections.Generic;

namespace Energinet.DataHub.MarketData.Infrastructure.DataPersistence.MarketEvaluationPoints
{
    public class MarketEvaluationPointDataModel : IDataModel
    {
        public MarketEvaluationPointDataModel(int id, string gsrnNumber, int type, List<RelationshipDataModel> relationships, bool productionObligated, int physicalState, int rowVersion)
        {
            Id = id;
            GsrnNumber = gsrnNumber;
            Type = type;
            Relationships = relationships;
            ProductionObligated = productionObligated;
            PhysicalState = physicalState;
            RowVersion = rowVersion;
        }

        public MarketEvaluationPointDataModel(int id, string gsrnNumber, bool productionObligated, int physicalState, int type, int rowVersion)
        {
            Id = id;
            GsrnNumber = gsrnNumber;
            Type = type;
            ProductionObligated = productionObligated;
            PhysicalState = physicalState;
            RowVersion = rowVersion;
            Relationships = new List<RelationshipDataModel>();
        }

        public int Id { get; set; }

        public string GsrnNumber { get; set; }

        public int Type { get; set; }

        public List<RelationshipDataModel> Relationships { get; }

        public bool ProductionObligated { get; set; }

        public int PhysicalState { get; set; }

        public int RowVersion { get; set; }
    }
}
