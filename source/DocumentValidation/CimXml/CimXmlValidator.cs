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

using System.Xml.Schema;
using DocumentValidation.Xml;

namespace DocumentValidation.CimXml;

public class CimXmlValidator : IValidator
{
    private readonly ISchemaProvider<XmlSchema> _schemaProvider;

    public CimXmlValidator(ISchemaProvider<XmlSchema> schemaProvider)
    {
        _schemaProvider = schemaProvider;
    }

    public DocumentFormat HandledFormat => DocumentFormat.CimXml;

    public async Task<ValidationResult> ValidateAsync(Stream document, DocumentType type, string version)
    {
        var schema = await _schemaProvider
            .GetAsync(type, version)
            .ConfigureAwait(false) ?? throw new InvalidOperationException($"Could find schema for {document}");
        return await XmlDocumentValidator.ValidateAsync(document, schema).ConfigureAwait(false);
    }
}
