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

using System.Xml;
using System.Xml.Schema;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.Schemas.Ebix;

public class EbixSchemaProvider : SchemaProvider, ISchemaProvider<XmlSchema>
{
    private readonly ISchema _schema = new EbixSchemas();
    private readonly Dictionary<string, XmlSchema> _schemaCache = new();

    public EbixSchemaProvider()
    {
        var notifyValidatedMeasureDataSchema = _schema.GetSchemaLocation(ParseDocumentType(DocumentType.NotifyValidatedMeasureData), "3")
            ?? throw new ArgumentException("Schema not found for DocumentType.NotifyValidatedMeasureData");
        LoadSchemaWithDependentSchemas(notifyValidatedMeasureDataSchema);
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

        if (string.IsNullOrEmpty(schemaName))
        {
            return Task.FromResult(default(T));
        }

        return (Task<T?>)(object)LoadSchemaWithDependentSchemasAsync<XmlSchema>(schemaName, cancellationToken);
    }

    protected override Task<T?> LoadSchemaWithDependentSchemasAsync<T>(
        string location, CancellationToken cancellationToken)
        where T : default
    {
        ArgumentNullException.ThrowIfNull(location);
        var loadSchemaWithDependentSchemas = LoadSchemaWithDependentSchemas(location);
        return Task.FromResult((T?)(object)loadSchemaWithDependentSchemas);
    }

    private static string ParseDocumentType(DocumentType document)
    {
        if (document == DocumentType.NotifyAggregatedMeasureData)
            return "DK_AggregatedMeteredDataTimeSeries";
        if (document == DocumentType.RejectRequestAggregatedMeasureData)
            return "DK_RejectRequestMeteredDataAggregated";
        if (document == DocumentType.NotifyWholesaleServices)
            return "DK_NotifyAggregatedWholesaleServices";
        if (document == DocumentType.RejectRequestWholesaleSettlement)
            return "DK_RejectAggregatedBillingInformation";
        if (document == DocumentType.NotifyValidatedMeasureData)
            return "DK_MeteredDataTimeSeries";
        if (document == DocumentType.Acknowledgement)
            return "DK_Acknowledgement";

        throw new InvalidOperationException($"Unknown document type: {document}");
    }

    private XmlSchema LoadSchemaWithDependentSchemas(string location)
    {
        ArgumentNullException.ThrowIfNull(location);

        // Ensure that only backslashes are used in paths
        location = location.Replace("/", "\\", StringComparison.InvariantCulture);
        if (_schemaCache.TryGetValue(location, out var cached))
            return cached;

        using var reader = new XmlTextReader(location);
        var xmlSchema = XmlSchema.Read(reader, null) ?? throw new XmlSchemaException($"Could not read schema at {location}");

        _schemaCache.TryAdd(location, xmlSchema);

        // Extract path of the current XSD as includes are relative to this
        var pathElements = location.Split('\\').ToList();
        pathElements.RemoveAt(pathElements.Count - 1);
        var relativeSchemaPath = string.Join("\\", pathElements) + "\\";

        foreach (XmlSchemaExternal external in xmlSchema.Includes)
        {
            if (external.SchemaLocation == null)
            {
                continue;
            }

            if (_schemaCache.TryGetValue(relativeSchemaPath + external.SchemaLocation, out var cachedExternal))
            {
                external.Schema = cachedExternal;
                continue;
            }

            external.Schema =
                LoadSchemaWithDependentSchemas(relativeSchemaPath + external.SchemaLocation);
        }

        return xmlSchema;
    }
}
