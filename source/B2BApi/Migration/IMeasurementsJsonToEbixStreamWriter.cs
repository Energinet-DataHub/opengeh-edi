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

using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.MarketDocuments;

namespace Energinet.DataHub.EDI.B2BApi.Migration;

public interface IMeasurementsJsonToEbixStreamWriter
{
    /// <summary>
    /// Transform imported JSON TimeSeries message to an ebiX stream writer format.
    /// </summary>
    /// <param name="timeSeriesPayload">Json payload containing all time series in message.</param>
    /// <returns>MarketDocumentStream containing all quantity observations.</returns>
    Task<Stream> WriteStreamAsync(BinaryData timeSeriesPayload);
}
