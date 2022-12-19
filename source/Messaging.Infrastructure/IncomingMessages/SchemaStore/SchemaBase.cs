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
using System.Linq;

namespace Messaging.Infrastructure.IncomingMessages.SchemaStore;

public abstract class SchemaBase
{
    private Dictionary<KeyValuePair<string, string>, string>? _schemas;

    protected SchemaBase()
    {
    }

    protected void InitializeSchemas(Dictionary<KeyValuePair<string, string>, string> schemas)
    {
        _schemas = new Dictionary<KeyValuePair<string, string>, string>();
        schemas.ToList().ForEach((x) =>
        {
            var keyToUpper = x.Key.Key.ToUpperInvariant();
            _schemas.Add(new KeyValuePair<string, string>(keyToUpper, x.Key.Value), x.Value);
        });
    }

    protected string? GetSchemaLocation(string businessProcessType, string version)
    {
        if (businessProcessType == null) throw new ArgumentNullException(nameof(businessProcessType));

        var schemaName = string.Empty;
        var businessProcessTypeToUpper = businessProcessType.ToUpperInvariant();

        if (_schemas == null) return schemaName;

        foreach (var key in _schemas.Keys)
        {
            if (key.Key.Equals(businessProcessTypeToUpper, StringComparison.OrdinalIgnoreCase))
            {
                _schemas.TryGetValue(
                    new KeyValuePair<string, string>(businessProcessTypeToUpper, version),
                    out schemaName);

                return schemaName;
            }
        }

        return schemaName;
    }

    protected abstract Dictionary<KeyValuePair<string, string>, string> FillSchemaDictionary(string schemaPath);
}
