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

using System.Text.Json;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.RSM012;
using NodaTime;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Migration;

public interface ITimeSeriesJsonToEbixTransformer
{
    /// <summary>
    /// Transform imported JSON TimeSeries message to a list of MeteredDataForMeteringPointMarketActivityRecord
    /// </summary>
    /// <param name="creation"></param>
    /// <param name="timeSeries">List containing all time series in message.</param>
    /// <returns>List of time series containing all quantity observations.</returns>
    List<MeteredDataForMeteringPointMarketActivityRecord> TransformJsonMessage(Instant creation, List<TimeSeries> timeSeries);
}
