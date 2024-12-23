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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.Wholesale.Edi.Validation.Helpers;

namespace Energinet.DataHub.Wholesale.Edi.Validation.AggregatedTimeSeriesRequest.Rules;

public class GridAreaValidationRule : IValidationRule<DataHub.Edi.Requests.AggregatedTimeSeriesRequest>
{
    private static readonly ValidationError _missingGridAreaCode = new("Netområde er obligatorisk for rollen MDR / Grid area is mandatory for the role MDR.", "D64");
    private static readonly ValidationError _invalidGridArea = new("Ugyldig netområde / Invalid gridarea", "E86");
    private readonly IMasterDataClient _masterDataClient;

    public GridAreaValidationRule(IMasterDataClient masterDataClient)
    {
        _masterDataClient = masterDataClient;
    }

    private static IList<ValidationError> NoError => new List<ValidationError>();

    private static IList<ValidationError> MissingGridAreaCodeError => new List<ValidationError> { _missingGridAreaCode };

    private static IList<ValidationError> InvalidGridAreaError => new List<ValidationError> { _invalidGridArea };

    public async Task<IList<ValidationError>> ValidateAsync(DataHub.Edi.Requests.AggregatedTimeSeriesRequest subject)
    {
        if (subject.RequestedForActorRole != DataHubNames.ActorRole.MeteredDataResponsible) return NoError;

        if (subject.GridAreaCodes.Count == 0)
            return MissingGridAreaCodeError;

        foreach (var gridAreaCode in subject.GridAreaCodes)
        {
            if (!await GridAreaValidationHelper.IsGridAreaOwnerAsync(_masterDataClient, gridAreaCode, subject.RequestedForActorNumber).ConfigureAwait(false))
                return InvalidGridAreaError;
        }

        return NoError;
    }
}
