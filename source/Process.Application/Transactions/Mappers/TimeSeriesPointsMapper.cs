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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.Process.Application.Transactions.Aggregations;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using Google.Protobuf.Collections;
using DecimalValue = Energinet.DataHub.Wholesale.Contracts.IntegrationEvents.Common.DecimalValue;

namespace Energinet.DataHub.EDI.Process.Application.Transactions.Mappers;

public static class TimeSeriesPointsMapper
{
    public static ReadOnlyCollection<Energinet.DataHub.EDI.Process.Domain.Transactions.Aggregations.OutgoingMessage.Point> MapPoints(RepeatedField<EnergyResultProducedV2.Types.TimeSeriesPoint> timeSeriesPoints)
    {
        ArgumentNullException.ThrowIfNull(timeSeriesPoints);

        var points = timeSeriesPoints
            .Select(
                (p, index) => new Energinet.DataHub.EDI.Process.Domain.Transactions.Aggregations.OutgoingMessage.Point(
                    index + 1, // Position starts at 1, so position = index + 1
                    DecimalValueMapper.Map(p.Quantity),
                    CalculatedQuantityQualityMapper.QuantityQualityCollectionToEdiQuality(p.QuantityQualities),
                    p.Time.ToString()))
            .ToList()
            .AsReadOnly();

        return points;
    }

    public static ReadOnlyCollection<Energinet.DataHub.EDI.Process.Domain.Transactions.Aggregations.OutgoingMessage.Point> MapPoints(IReadOnlyCollection<Domain.Transactions.AggregatedMeasureData.Point> points)
    {
        return points
            .Select(p => new Energinet.DataHub.EDI.Process.Domain.Transactions.Aggregations.OutgoingMessage.Point(p.Position, p.Quantity, p.QuantityQuality, p.SampleTime))
            .ToList()
            .AsReadOnly();
    }
}
