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

using System.Threading.Tasks;
using System.Xml.Schema;
using Json.Schema;
using Messaging.Application.SchemaStore;
using Xunit;

namespace Messaging.Tests;

public class SchemaProviderTests : TestBase
{
    private readonly XmlSchemaProvider _xmlSchemaProvider;
    private readonly JsonSchemaProvider _jsonSchemaProvider;

    public SchemaProviderTests()
    {
        _xmlSchemaProvider = GetService<XmlSchemaProvider>();
        _jsonSchemaProvider = GetService<JsonSchemaProvider>();
    }

    [Fact]
    public async Task Schema_provider_can_get_xml_schema()
    {
        var schema = await _xmlSchemaProvider.GetSchemaAsync<XmlSchema>("confirmrequestchangeofsupplier", "0.1").ConfigureAwait(false);
        Assert.Equal(typeof(XmlSchema), schema?.GetType());
    }

    [Fact]
    public async Task Schema_provider_can_get_json_schema()
    {
        var schema = await _jsonSchemaProvider.GetSchemaAsync<JsonSchema>("RequestChangeofSupplier", "0").ConfigureAwait(false);
        Assert.Equal(typeof(JsonSchema), schema?.GetType());
    }
}
