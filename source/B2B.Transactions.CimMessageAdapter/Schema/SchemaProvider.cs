using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using B2B.CimMessageAdapter.Schema;

namespace B2B.CimMessageAdapter
{
    public class SchemaProvider : ISchemaProvider
    {
        private readonly SchemaStore _schemaStore;

        public SchemaProvider(SchemaStore schemaStore)
        {
            _schemaStore = schemaStore;
        }

        public Task<XmlSchema?> GetSchemaAsync(string businessProcessType, string version)
        {
            var schemaName = _schemaStore.GetSchemaLocation(businessProcessType, version);

            if (schemaName == null)
            {
                return Task.FromResult(default(XmlSchema));
            }

            return LoadSchemaWithDependentSchemasAsync(schemaName);
        }

        private static async Task<XmlSchema?> LoadSchemaWithDependentSchemasAsync(string location)
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
                    await LoadSchemaWithDependentSchemasAsync(external.SchemaLocation).ConfigureAwait(false);
            }

            return xmlSchema;
        }
    }
}
