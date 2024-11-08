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

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParsers;

public abstract class MessageParser
{
    private readonly Dictionary<DocumentFormat, Func<IIncomingMarketMessageStream, CancellationToken, Task<IncomingMarketMessageParserResult>>> _parsingMethods;

    protected MessageParser()
    {
        _parsingMethods = new Dictionary<DocumentFormat, Func<IIncomingMarketMessageStream, CancellationToken, Task<IncomingMarketMessageParserResult>>>
        {
            { DocumentFormat.Ebix, ParseEbixXmlAsync },
            { DocumentFormat.Json, ParseJsonAsync },
            { DocumentFormat.Xml, ParseXmlAsync },
        };
    }

    public async Task<IncomingMarketMessageParserResult> ParseAsync(
        IIncomingMarketMessageStream marketMessage,
        DocumentFormat documentFormat,
        CancellationToken cancellationToken)
    {
        if (_parsingMethods.TryGetValue(documentFormat, out var parseMethod))
        {
            return await parseMethod(marketMessage, cancellationToken).ConfigureAwait(false);
        }

        throw new NotSupportedException($"No message parser found for message format '{documentFormat}'");
    }

    protected abstract Task<IncomingMarketMessageParserResult> ParseXmlAsync(
        IIncomingMarketMessageStream incomingMarketMessageStream, CancellationToken cancellationToken);

    protected abstract Task<IncomingMarketMessageParserResult> ParseJsonAsync(
        IIncomingMarketMessageStream incomingMarketMessageStream, CancellationToken cancellationToken);

    protected abstract Task<IncomingMarketMessageParserResult> ParseEbixXmlAsync(
        IIncomingMarketMessageStream incomingMarketMessageStream, CancellationToken cancellationToken);
}
