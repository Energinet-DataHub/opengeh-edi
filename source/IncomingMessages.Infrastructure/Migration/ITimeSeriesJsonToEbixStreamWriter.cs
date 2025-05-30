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
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.MarketDocuments;
using NodaTime;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Migration;

public interface ITimeSeriesJsonToEbixStreamWriter
{
    /// <summary>
    /// Transform imported JSON TimeSeries message to an EbiX stream writer format.
    /// </summary>
    /// <param name="timeSeriesPayload">List containing all time series in message.</param>
    /// <returns>MarketDocumentStream containing all quantity observations.</returns>
    Task<MarketDocumentStream> WriteStreamAsync(string timeSeriesPayload);
}
