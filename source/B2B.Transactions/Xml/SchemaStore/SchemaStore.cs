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

using System.Collections.Generic;
using System.IO;

namespace B2B.Transactions.Xml.Incoming
{
    public class SchemaStore
    {
        public SchemaStore()
        {
            Schemas = new Dictionary<KeyValuePair<string, string>, string>
            {
                {
                    new KeyValuePair<string, string>("requestchangeofsupplier", "1.0"),
                    SchemaPath + "urn-ediel-org-structure-requestchangeofsupplier-0-1.xsd"
                },
            };
        }

        public static string SchemaPath => $"Xml{Path.DirectorySeparatorChar}SchemaStore{Path.DirectorySeparatorChar}Schemas{Path.DirectorySeparatorChar}";

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
