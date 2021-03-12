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

using Energinet.DataHub.MarketData.Domain.SeedWork;

namespace Energinet.DataHub.MarketData.Domain.MeteringPoints
{
    public class MeteringPointType : EnumerationType
    {
        public static readonly MeteringPointType Consumption = new MeteringPointType(0, nameof(Consumption));
        public static readonly MeteringPointType Production = new MeteringPointType(1, nameof(Production));
        public static readonly MeteringPointType Exchange = new MeteringPointType(2, nameof(Exchange));

        public MeteringPointType(int id, string name)
            : base(id, name)
        {
        }
    }
}
