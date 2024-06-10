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

namespace Energinet.DataHub.EDI.IncomingMessages.Application.MessageParser;

public class MarketMessageParser
{
    private readonly IEnumerable<IMessageParser> _parsers;

    public MarketMessageParser(IEnumerable<IMessageParser> parsers)
    {
        _parsers = parsers;
    }

    public Task<IncomingMarketMessageParserResult> ParseAsync(
        IIncomingMessageStream message,
        DocumentFormat documentFormat,
        IncomingDocumentType documentType,
        CancellationToken cancellationToken)
    {
        var parser = _parsers.FirstOrDefault(parser =>
            parser.HandledFormat.Equals(documentFormat) && parser.DocumentType.Equals(documentType));
        if (parser is null)
            throw new InvalidOperationException($"No message parser found for message format '{documentFormat}'");
        return parser.ParseAsync(message, cancellationToken);
    }
}
