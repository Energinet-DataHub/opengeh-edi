using System.Collections.Generic;

namespace B2B.CimMessageAdapter.Schema
{
    public class SchemaStore
    {
        public SchemaStore()
        {
            Schemas = new Dictionary<KeyValuePair<string, string>, string>
            {
                {
                    new KeyValuePair<string, string>("requestchangeofsupplier", "1.0"),
                    "urn-ediel-org-structure-requestchangeofsupplier-0-1.xsd"
                },
            };
        }

        public Dictionary<KeyValuePair<string, string>, string> Schemas { get; }

        public string? GetSchemaLocation(string businessProcessType, string version)
        {
            Schemas.TryGetValue(
                new KeyValuePair<string, string>(businessProcessType, version),
                out var schemaName);

            return schemaName;
        }
    }
}
