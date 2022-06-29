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
using System.Net.Mime;

namespace Messaging.Application.SchemaStore;

public static class SchemaProviderFactory
{
    public static ISchemaProvider GetProvider(string? contentType)
    {
        if (contentType == null) throw new ArgumentNullException(nameof(contentType));

        return contentType.Equals(MediaTypeNames.Application.Json, StringComparison.OrdinalIgnoreCase)
            ? new JsonSchemaProvider() : new XmlSchemaProvider();
    }
}
