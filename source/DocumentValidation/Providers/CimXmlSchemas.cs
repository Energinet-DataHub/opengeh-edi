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

namespace DocumentValidation.Providers
{
    public sealed class CimXmlSchemas : SchemaBase, ISchema, ISchemaProvider<XmlSchema>
    {
        private static readonly string _schemaPath = $"Schemas{Path.DirectorySeparatorChar}Xml{Path.DirectorySeparatorChar}";

        public CimXmlSchemas()
        {
            InitializeSchemas(FillSchemaDictionary(_schemaPath));
        }

        public string SchemaPath => _schemaPath;

        string? ISchema.GetSchemaLocation(string businessProcessType, string version)
        {
            return GetSchemaLocation(businessProcessType, version);
        }

        public async Task<XmlSchema> GetSchemaForAsync(SchemaDetails details)
        {
            ArgumentNullException.ThrowIfNull(details);

            using var reader = new XmlTextReader(details.Location);
            var xmlSchema = XmlSchema.Read(reader, null);
            if (xmlSchema is null)
            {
                throw new XmlSchemaException($"Could not read schema at {details.Location}");
            }

            foreach (XmlSchemaExternal external in xmlSchema.Includes)
            {
                if (external.SchemaLocation == null)
                {
                    continue;
                }

                external.Schema =
                    await LoadSchemaWithDependentSchemasAsync(_schemaPath + external.SchemaLocation).ConfigureAwait(false);
            }

            return xmlSchema;
        }

        protected override Dictionary<KeyValuePair<string, string>, string> FillSchemaDictionary(string schemaPath)
        {
            var schemaDictionary = new Dictionary<KeyValuePair<string, string>, string>();
            var schemas = Directory.GetFiles(schemaPath).ToList();
            foreach (var schema in schemas)
            {
                var filename = Path.GetFileNameWithoutExtension(schema);
                var filenameSplit = filename.Split('-');
                if (filenameSplit.Length == 7)
                {
                    schemaDictionary.Add(
                        new KeyValuePair<string, string>(filenameSplit[4], filenameSplit[5] + "." + filenameSplit[6]),
                        schema);
                    AddDetails(new SchemaDetails(
                        filenameSplit[4],
                        filenameSplit[5] + "." + filenameSplit[6],
                        schema));
                }
            }

            return schemaDictionary;
        }

        private async Task<XmlSchema> LoadSchemaWithDependentSchemasAsync(string location)
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
                    await LoadSchemaWithDependentSchemasAsync(_schemaPath + external.SchemaLocation).ConfigureAwait(false);
            }

            return xmlSchema;
        }
    }
}
