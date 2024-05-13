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

using System;
using System.Collections.ObjectModel;
using System.Xml;
using System.Xml.Schema;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.ValidationErrors;

namespace Energinet.DataHub.EDI.IncomingMessages.Application.MessageParser.BaseParsers;

public abstract class XmlBaseParser
{
    protected Collection<ValidationError> Errors { get; } = new();

    protected static string GetVersion(IIncomingMessageStream message)
    {
        ArgumentNullException.ThrowIfNull(message);
        var split = SplitNamespace(message);
        var version = split[4] + "." + split[5];
        return version;
    }

    protected static string GetBusinessReason(IIncomingMessageStream message)
    {
        ArgumentNullException.ThrowIfNull(message);
        var split = SplitNamespace(message);
        var businessReason = split[3];
        return businessReason;
    }

    protected static IncomingMarketMessageParserResult InvalidXmlFailure(
        Exception exception)
    {
        return new IncomingMarketMessageParserResult(
            InvalidMessageStructure.From(exception));
    }

    protected XmlReaderSettings CreateXmlReaderSettings(XmlSchema xmlSchema)
    {
        var settings = new XmlReaderSettings
        {
            Async = true,
            ValidationType = ValidationType.Schema,
            ValidationFlags = XmlSchemaValidationFlags.ProcessInlineSchema |
                              XmlSchemaValidationFlags.ReportValidationWarnings,
        };

        settings.Schemas.Add(xmlSchema);
        settings.ValidationEventHandler += OnValidationError;
        return settings;
    }

    private static string[] SplitNamespace(IIncomingMessageStream message)
    {
        ArgumentNullException.ThrowIfNull(message);

        using var reader = XmlReader.Create(message.Stream);

        var split = Array.Empty<string>();
        while (reader.Read())
        {
            if (string.IsNullOrEmpty(reader.NamespaceURI)) continue;
            var @namespace = reader.NamespaceURI;
            split = @namespace.Split(':');
            break;
        }

        return split;
    }

    private void OnValidationError(object? sender, ValidationEventArgs arguments)
    {
        var message =
            $"XML schema validation error at line {arguments.Exception.LineNumber}, position {arguments.Exception.LinePosition}: {arguments.Message}.";
        Errors.Add(InvalidMessageStructure.From(message));
    }
}
