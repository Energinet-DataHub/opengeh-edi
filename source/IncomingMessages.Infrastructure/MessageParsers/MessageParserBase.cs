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

using System.Collections.ObjectModel;
using System.Xml;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Validation.ValidationErrors;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Schemas;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParsers;

public abstract class MessageParserBase<TSchema>() : IMessageParser
{
    protected Collection<ValidationError> Errors { get; } = [];

    public async Task<IncomingMarketMessageParserResult> ParseAsync(
        IIncomingMarketMessageStream marketMessage,
        CancellationToken cancellationToken)
    {
        var schemaResult = await GetSchemaAsync(marketMessage, cancellationToken).ConfigureAwait(false);
        if (schemaResult.Schema == null)
        {
            return schemaResult.Result ?? new IncomingMarketMessageParserResult(new InvalidSchemaOrNamespace());
        }

        return await ParseMessageAsync(marketMessage, schemaResult.Schema, cancellationToken)
            .ConfigureAwait(false);
    }

    protected static IncomingMarketMessageParserResult Invalid(
        Exception exception)
    {
        return new IncomingMarketMessageParserResult(
            InvalidMessageStructure.From(exception));
    }

    protected abstract Task<(TSchema? Schema, IncomingMarketMessageParserResult? Result)>
        GetSchemaAsync(IIncomingMarketMessageStream marketMessage, CancellationToken cancellationToken);

    protected abstract Task<IncomingMarketMessageParserResult> ParseMessageAsync(
        IIncomingMarketMessageStream marketMessage,
        TSchema schemaResult,
        CancellationToken cancellationToken);
}
