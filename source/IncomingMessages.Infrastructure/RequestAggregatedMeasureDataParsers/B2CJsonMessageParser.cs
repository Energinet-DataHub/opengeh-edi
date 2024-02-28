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
using Energinet.DataHub.EDI.Common.Serialization;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Messages;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Messages.RequestAggregatedMeasureData;
using Energinet.DataHub.EDI.Process.Interfaces;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.RequestAggregatedMeasureDataParsers;

public class B2CJsonMessageParser : IMessageParser
{
    private readonly ISerializer _serializer;

    public B2CJsonMessageParser(
        ISerializer serializer)
        : base()
    {
        _serializer = serializer;
    }

    public DocumentFormat HandledFormat => DocumentFormat.Json;

    public IncomingDocumentType DocumentType => IncomingDocumentType.B2CRequestAggregatedMeasureData;

    public async Task<RequestAggregatedMeasureDataMarketMessageParserResult> ParseAsync(
        IIncomingMessageStream incomingMessageStream,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(incomingMessageStream);

        var requestAggregatedMeasureData = await _serializer.DeserializeAsync<RequestAggregatedMeasureDataDto>(incomingMessageStream.Stream, cancellationToken).ConfigureAwait(false);
        return new RequestAggregatedMeasureDataMarketMessageParserResult(requestAggregatedMeasureData);
    }
}
