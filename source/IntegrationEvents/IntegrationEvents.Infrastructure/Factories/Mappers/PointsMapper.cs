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
using System.Collections.ObjectModel;
using System.Linq;
using Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.Protobuf;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using Google.Protobuf.Collections;

namespace Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.Factories.Mappers;

public static class PointsMapper
{
    public static ReadOnlyCollection<EnergyResultMessagePoint> MapPoints(RepeatedField<EnergyResultProducedV2.Types.TimeSeriesPoint> timeSeriesPoints)
    {
        ArgumentNullException.ThrowIfNull(timeSeriesPoints);

        var points = timeSeriesPoints
            .Select(
                (p, index) => new EnergyResultMessagePoint(
                    index + 1, // Position starts at 1, so position = index + 1
                    DecimalParser.Parse(p.Quantity),
                    CalculatedQuantityQualityMapper.QuantityQualityCollectionToEdiQuality(p.QuantityQualities),
                    p.Time.ToString()))
            .ToList()
            .AsReadOnly();

        return points;
    }

    public static IReadOnlyCollection<WholesaleServicesPoint> MapPoints(RepeatedField<AmountPerChargeResultProducedV1.Types.TimeSeriesPoint> timeSeriesPoints)
    {
        var points = timeSeriesPoints
            .Select(
                (p, index) => new WholesaleServicesPoint(
                    index + 1, // Position starts at 1, so position = index + 1
                    DecimalParser.Parse(p.Quantity),
                    p.Price == null ? null : DecimalParser.Parse(p.Price),
                    p.Amount == null ? null : DecimalParser.Parse(p.Amount),
                    CalculatedQuantityQualityMapper.QuantityQualityCollectionToEdiQuality(p.QuantityQualities)))
            .ToList()
            .AsReadOnly();

        return points;
    }
}
