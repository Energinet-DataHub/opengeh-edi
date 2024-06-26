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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.Protobuf;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents.Common;
using Google.Protobuf.Collections;

namespace Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.Factories.Mappers;

public static class PointsMapper
{
    public static ReadOnlyCollection<EnergyResultMessagePoint> Map(RepeatedField<EnergyResultProducedV2.Types.TimeSeriesPoint> timeSeriesPoints)
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

    public static IReadOnlyCollection<WholesaleServicesPoint> Map(
        RepeatedField<AmountPerChargeResultProducedV1.Types.TimeSeriesPoint> timeSeriesPoints,
        AmountPerChargeResultProducedV1.Types.ChargeType chargeType)
    {
        var points = timeSeriesPoints
            .Select(
                (p, index) => new WholesaleServicesPoint(
                    index + 1, // Position starts at 1, so position = index + 1
                    DecimalParser.Parse(p.Quantity),
                    p.Price == null ? null : DecimalParser.Parse(p.Price),
                    p.Amount == null ? null : DecimalParser.Parse(p.Amount),
                    GetQuantityQuality(p.Price, p.QuantityQualities, chargeType)))
            .ToList()
            .AsReadOnly();

        return points;
    }

    /// <summary>
    /// Quantity quality mappings is defined by the business.
    /// See "https://energinet.atlassian.net/wiki/spaces/D3/pages/529989633/QuantityQuality" for more information.
    /// </summary>
    private static CalculatedQuantityQuality GetQuantityQuality(DecimalValue? price, RepeatedField<AmountPerChargeResultProducedV1.Types.QuantityQuality> quantityQualities, AmountPerChargeResultProducedV1.Types.ChargeType chargeType)
    {
        if (price == null)
        {
            return CalculatedQuantityQuality.Missing;
        }

        if (chargeType == AmountPerChargeResultProducedV1.Types.ChargeType.Subscription || chargeType == AmountPerChargeResultProducedV1.Types.ChargeType.Fee)
        {
            return CalculatedQuantityQuality.Calculated;
        }

        return CalculatedQuantityQualityMapper.QuantityQualityCollectionToEdiQuality(quantityQualities);
    }
}
