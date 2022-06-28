using System.Net.Mime;
using System.Threading.Tasks;
using System.Xml.Schema;
using Messaging.Application.SchemaStore;
using NJsonSchema;
using Xunit;

namespace Messaging.Tests;

public class SchemaProviderTests
{
    private ISchemaProvider? _provider;

    [Fact]
    public async Task Schema_provider_can_get_xml_schema()
    {
        _provider = SchemaProviderFactory.GetProvider(MediaTypeNames.Application.Xml);
        var schema = await _provider.GetSchemaAsync<XmlSchema>("confirmrequestchangeofsupplier", "0.1").ConfigureAwait(false);
        Assert.Equal(typeof(XmlSchema), schema?.GetType());
    }

    [Fact]
    public async Task Schema_provider_can_get_json_schema()
    {
        _provider = SchemaProviderFactory.GetProvider(MediaTypeNames.Application.Json);
        var schema = await _provider.GetSchemaAsync<JsonSchema>("Request-Change-of-Supplier-assembly-model.schema", "0").ConfigureAwait(false);
        Assert.Equal(typeof(JsonSchema), schema?.GetType());
    }
}
