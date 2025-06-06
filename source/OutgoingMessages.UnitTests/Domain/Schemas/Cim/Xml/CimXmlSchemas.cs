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

namespace Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.Schemas.Cim.Xml;

public sealed class CimXmlSchemas : SchemaBase, ISchema
{
    private static readonly string _schemaPath = $"Domain{Path.DirectorySeparatorChar}Schemas{Path.DirectorySeparatorChar}Cim{Path.DirectorySeparatorChar}Xml{Path.DirectorySeparatorChar}Schemas{Path.DirectorySeparatorChar}";

    public CimXmlSchemas()
    {
        InitializeSchemas(FillSchemaDictionary(_schemaPath));
    }

    public string SchemaPath => _schemaPath;

    string? ISchema.GetSchemaLocation(string businessProcessType, string version)
    {
        return GetSchemaLocation(businessProcessType, version);
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
            }
        }

        return schemaDictionary;
    }
}
