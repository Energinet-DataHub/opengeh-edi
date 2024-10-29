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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

public sealed class BusinessReason : DataHubTypeWithUnused<BusinessReason>
{
    public static readonly BusinessReason MoveIn = new(DataHubNames.BusinessReason.MoveIn, "E65");
    public static readonly BusinessReason BalanceFixing = new(DataHubNames.BusinessReason.BalanceFixing, "D04");
    public static readonly BusinessReason PreliminaryAggregation = new(DataHubNames.BusinessReason.PreliminaryAggregation, "D03");
    public static readonly BusinessReason WholesaleFixing = new(DataHubNames.BusinessReason.WholesaleFixing, "D05"); // Engrosfiksering
    public static readonly BusinessReason Correction = new(DataHubNames.BusinessReason.Correction, "D32");
    public static readonly BusinessReason PeriodicMetering = new(DataHubNames.BusinessReason.PeriodicMetering, "E23");
    public static readonly BusinessReason PeriodicFlexMetering = new(DataHubNames.BusinessReason.PeriodicFlexMetering, "D42");

    private BusinessReason(string name, string code, bool isUnused = false)
     : base(name, code, isUnused) { }
}
