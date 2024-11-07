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

using BuildingBlocks.Application.FeatureFlag;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParsers;

public class MarketMessageParser(
    IEnumerable<IMarketMessageParser> parsers,
    IDictionary<IncomingDocumentType, IMessageParser> messageParsers,
    IFeatureFlagManager featureFlagManager)
{
    private readonly Dictionary<DocumentFormat, Func<IMessageParser, IIncomingMarketMessageStream, CancellationToken, Task<IncomingMarketMessageParserResult>>> _parsingMethods =
        new()
        {
            { DocumentFormat.Ebix, (parser, message, token) => parser.ParseEbixXmlAsync(message, token) },
            { DocumentFormat.Json, (parser, message, token) => parser.ParseJsonAsync(message, token) },
            { DocumentFormat.Xml, (parser, message, token) => parser.ParseXmlAsync(message, token) },
        };

    private readonly IEnumerable<IMarketMessageParser> _parsers = parsers;
    private readonly IDictionary<IncomingDocumentType, IMessageParser> _messageParsers = messageParsers;
    private readonly IFeatureFlagManager _featureFlagManager = featureFlagManager;

    public async Task<IncomingMarketMessageParserResult> ParseAsync(
        IIncomingMarketMessageStream marketMessage,
        DocumentFormat documentFormat,
        IncomingDocumentType documentType,
        CancellationToken cancellationToken)
    {
        if (await _featureFlagManager.UseNewIncomingMessageParserAsync().ConfigureAwait(false))
        {
            if (_messageParsers.TryGetValue(documentType, out var messageParser) &&
                _parsingMethods.TryGetValue(documentFormat, out var parse))
            {
                return await parse(messageParser, marketMessage, cancellationToken).ConfigureAwait(false);
            }
        }

        var parser = _parsers.FirstOrDefault(parser =>
            parser.HandledFormat.Equals(documentFormat) && parser.DocumentType.Equals(documentType));
        if (parser is null)
            throw new NotSupportedException($"No message parser found for message format '{documentFormat}' and document type '{documentType}'");
        return await parser.ParseAsync(marketMessage, cancellationToken).ConfigureAwait(false);
    }
}
