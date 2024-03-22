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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.Process.Infrastructure.Transactions;

public static class WholesaleRequestNameConverter
{
    public static string ActorRoleCodeToName(string actorRoleCode)
    {
        return ActorRole.TryGetNameFromCode(actorRoleCode) ?? actorRoleCode;
    }

    public static string MeteringPointTypeCodeToName(string meteringPointTypeCode)
    {
        return MeteringPointType.TryGetNameFromCode(meteringPointTypeCode) ?? meteringPointTypeCode;
    }

    public static string SettlementTypeCodeToName(string settlementTypeCode)
    {
        return SettlementType.TryGetNameFromCode(settlementTypeCode) ?? settlementTypeCode;
    }

    public static string? ChargeTypeCodeToName(string? chargeTypeCode)
    {
        if (chargeTypeCode == null) return null;

        return ChargeType.TryGetNameFromCode(chargeTypeCode) ?? chargeTypeCode;
    }

    public static string ResolutionCodeToName(string resolutionCode)
    {
        return Resolution.TryGetNameFromCode(resolutionCode) ?? resolutionCode;
    }
}
