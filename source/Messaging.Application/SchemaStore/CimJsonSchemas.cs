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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Json.Schema;

namespace Messaging.Application.SchemaStore;

public sealed class CimJsonSchemas : SchemaBase, ISchema
{
    private const string SchemaBaseUri = @"file:///C:/Users/Public/Documents/iec.ch/TC57/2020/";
    private static readonly string _schemaPath = $"SchemaStore{Path.DirectorySeparatorChar}Schemas{Path.DirectorySeparatorChar}Json{Path.DirectorySeparatorChar}";
    private static readonly Mutex _mutex = new();

    public CimJsonSchemas()
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
            if (filename.Contains("assembly", StringComparison.OrdinalIgnoreCase))
            {
                var split = filename.Substring(0, filename.IndexOf("assembly", StringComparison.OrdinalIgnoreCase));
                var splitArray = split.Split('-');
                filename = string.Join(string.Empty, splitArray);
            }

            schemaDictionary.Add(
                    new KeyValuePair<string, string>(filename, "0"),
                    schema);

            RegisterSchema(schema);
        }

        return schemaDictionary;
    }

    private static void RegisterSchema(string schemaPath)
    {
        if (_mutex.WaitOne(1000))
        {
            var schema = JsonSchema.FromFile(schemaPath);
            var schemaName = Path.GetFileName(schemaPath);
            SchemaRegistry.Global.Register(new Uri(SchemaBaseUri + schemaName), schema);
            _mutex.ReleaseMutex();
        }
        else
        {
            throw new InvalidOperationException($"Unable to add JSON schema {schemaPath} to global registry");
        }
    }
}
