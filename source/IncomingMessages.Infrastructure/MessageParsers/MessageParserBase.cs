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

public abstract class MessageParserBase<TSchema>(ISchemaProvider schemaProvider) : IMessageParser
{
    private readonly ISchemaProvider _schemaProvider = schemaProvider;

    protected Collection<ValidationError> Errors { get; } = [];

    public async Task<IncomingMarketMessageParserResult> ParseAsync(
        IIncomingMarketMessageStream marketMessage,
        CancellationToken cancellationToken)
    {
        var schemaResult = await GetSchemaAsync(marketMessage, cancellationToken).ConfigureAwait(false);
        if (schemaResult.Schema == null || schemaResult.Namespace == null)
        {
            return schemaResult.Result ?? new IncomingMarketMessageParserResult(new InvalidSchemaOrNamespace());
        }

        return await ParseMessageAsync(marketMessage, schemaResult.Schema, schemaResult.Namespace, cancellationToken)
            .ConfigureAwait(false);
    }

    protected abstract string BusinessProcessType(string ns);

    protected abstract string GetVersion(string ns);

    protected abstract string GetNamespace(IIncomingMarketMessageStream marketMessage);

    protected abstract Task<IncomingMarketMessageParserResult> ParseMessageAsync(
        IIncomingMarketMessageStream marketMessage,
        TSchema schemaResult,
        string @namespace,
        CancellationToken cancellationToken);

    private static IncomingMarketMessageParserResult Invalid(
        Exception exception)
    {
        return new IncomingMarketMessageParserResult(
            InvalidMessageStructure.From(exception));
    }

    private async Task<(TSchema? Schema, string? Namespace, IncomingMarketMessageParserResult? Result)> GetSchemaAsync(IIncomingMarketMessageStream marketMessage, CancellationToken cancellationToken)
    {
        string? @namespace = null;
        IncomingMarketMessageParserResult? parserResult = null;
        TSchema? xmlSchema = default;
        try
        {
            @namespace = GetNamespace(marketMessage);
            var version = GetVersion(@namespace);
            var businessProcessType = BusinessProcessType(@namespace);
            xmlSchema = await _schemaProvider.GetSchemaAsync<TSchema>(businessProcessType, version, cancellationToken)
                .ConfigureAwait(true);

            if (xmlSchema is null)
            {
                parserResult = new IncomingMarketMessageParserResult(
                    new InvalidBusinessReasonOrVersion(businessProcessType, version));
            }
        }
        catch (XmlException exception)
        {
            parserResult = Invalid(exception);
        }
        catch (ObjectDisposedException objectDisposedException)
        {
            parserResult = Invalid(objectDisposedException);
        }
        catch (IndexOutOfRangeException indexOutOfRangeException)
        {
            parserResult = Invalid(indexOutOfRangeException);
        }

        return (xmlSchema, @namespace, parserResult);
    }
}
