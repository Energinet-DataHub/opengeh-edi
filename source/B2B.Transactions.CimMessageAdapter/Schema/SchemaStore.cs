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
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace B2B.CimMessageAdapter.Schema
{
    public class SchemaStore
    {
        public static string SchemaPath => $"Schema{Path.DirectorySeparatorChar}Schemas{Path.DirectorySeparatorChar}";

        [SuppressMessage(
            "StyleCop.CSharp.OrderingRules",
            "SA1201:Elements should appear in the correct order",
            Justification = "Stupid rule that properties cant come before constructor")]
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
