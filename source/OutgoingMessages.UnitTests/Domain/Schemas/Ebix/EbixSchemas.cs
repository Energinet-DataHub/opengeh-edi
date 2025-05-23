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

namespace Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.Schemas.Ebix;

public sealed class EbixSchemas : SchemaBase, ISchema
{
    private static readonly string _schemaPath = $"Domain{Path.DirectorySeparatorChar}Schemas{Path.DirectorySeparatorChar}Ebix{Path.DirectorySeparatorChar}Schemas{Path.DirectorySeparatorChar}documents{Path.DirectorySeparatorChar}";

    public EbixSchemas()
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
        var directories = Directory.GetDirectories(schemaPath);
        foreach (var directory in directories)
        {
            var schemas = Directory.GetFiles(directory).ToList();
            foreach (var schema in schemas)
            {
                var filename = Path.GetFileNameWithoutExtension(schema);
                filename = filename.Replace("-", "_", StringComparison.InvariantCulture);
                var filenameSplit = filename.Split('_');
                if (filenameSplit.Length >= 3 && filenameSplit[0] == "ebIX" && filenameSplit[1] == "DK")
                {
                    schemaDictionary.Add(
                        new KeyValuePair<string, string>(filenameSplit[1] + "_" + filenameSplit[2], "3"),
                        schema);
                }
            }
        }

        return schemaDictionary;
    }
}
