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

using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.WholesaleResultMessages;

namespace Energinet.DataHub.EDI.Process.Application.Transactions.WholesaleServices.Mappers;

public static class PointsMapper
{
    public static IReadOnlyCollection<WholesaleServicesPoint> Map(IReadOnlyCollection<Point> timeSeriesPoints)
    {
        var points = timeSeriesPoints
            .Select(
                (p, index) => new WholesaleServicesPoint(
                    index + 1, // Position starts at 1, so position = index + 1
                    p.Quantity,
                    p.Price,
                    p.Amount,
                    p.QuantityQuality))
            .ToList()
            .AsReadOnly();

        return points;
    }
}
