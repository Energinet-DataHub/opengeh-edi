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

using Energinet.DataHub.EDI.Common;

namespace IncomingMessages.Infrastructure.Messages.RequestAggregatedMeasureData;

public class RequestAggregatedMeasureDataMarketMessageParser : IRequestAggregatedMeasureDataMarketMessageParser
{
    private readonly IEnumerable<IMessageParser> _parsers;

    public RequestAggregatedMeasureDataMarketMessageParser(IEnumerable<IMessageParser> parsers)
    {
        _parsers = parsers;
    }

    public Task<RequestAggregatedMeasureDataMarketMessageParserResult> ParseAsync(
        Stream message, DocumentFormat documentFormat, CancellationToken cancellationToken)
    {
        var parser = _parsers.FirstOrDefault(parser => parser.HandledFormat.Equals(documentFormat));
        if (parser is null)
            throw new InvalidOperationException($"No message parser found for message format '{documentFormat}'");
        return parser.ParseAsync(message, cancellationToken);
    }
}
