﻿// Copyright 2020 Energinet DataHub A/S
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

using System.Xml;
using System.Xml.Schema;

namespace Energinet.DataHub.EDI.Infrastructure.DocumentValidation.CimXml;

public class CimXmlSchemaProvider : SchemaProvider, ISchemaProvider<XmlSchema>
{
    private readonly ISchema _schema;

    public CimXmlSchemaProvider()
    {
        _schema = new CimXmlSchemas();
    }

    public Task<XmlSchema?> GetAsync(DocumentType type, string version, CancellationToken cancellationToken)
    {
        var schemaName = _schema.GetSchemaLocation(ParseDocumentType(type), version);

        if (schemaName == null)
        {
            return Task.FromResult(default(XmlSchema));
        }

        return LoadSchemaWithDependentSchemasAsync<XmlSchema>(schemaName, cancellationToken);
    }

    public override Task<T?> GetSchemaAsync<T>(
        string businessProcessType, string version, CancellationToken cancellationToken)
        where T : default
    {
        var schemaName = _schema.GetSchemaLocation(businessProcessType, version);

        if (schemaName == null)
        {
            return Task.FromResult(default(T));
        }

        return (Task<T?>)(object)LoadSchemaWithDependentSchemasAsync<XmlSchema>(schemaName, cancellationToken);
    }

    protected override async Task<T?> LoadSchemaWithDependentSchemasAsync<T>(
        string location, CancellationToken cancellationToken)
        where T : default
    {
        using var reader = new XmlTextReader(location);
        var xmlSchema = XmlSchema.Read(reader, null);
        if (xmlSchema is null)
        {
            throw new XmlSchemaException($"Could not read schema at {location}");
        }

        foreach (XmlSchemaExternal external in xmlSchema.Includes)
        {
            if (external.SchemaLocation == null)
            {
                continue;
            }

            external.Schema =
                await LoadSchemaWithDependentSchemasAsync<XmlSchema>(
                        _schema.SchemaPath + external.SchemaLocation, cancellationToken)
                    .ConfigureAwait(false);
        }

        return (T)(object)xmlSchema;
    }

    private static string ParseDocumentType(DocumentType document)
    {
        return document switch
        {
            DocumentType.AggregationResult => "notifyaggregatedmeasuredata",
            DocumentType.CustomerMasterData => "CharacteristicsOfACustomerAtAnAp",
            DocumentType.RejectRequestAggregatedMeasureData => "RejectRequestAggregatedMeasureData",
            _ => throw new InvalidOperationException("Unknown document type"),
        };
    }
}
