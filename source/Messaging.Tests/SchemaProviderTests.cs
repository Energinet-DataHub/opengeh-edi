using System.Net.Mime;
using System.Threading.Tasks;
using System.Xml.Schema;
using Messaging.Application.SchemaStore;
using Xunit;

namespace Messaging.Tests;

public class SchemaProviderTests
{
    [Fact]
    public async Task SomeTest()
    {
        var xmlProvider = SchemaProviderFactory.GetProvider(MediaTypeNames.Application.Xml);
        var schema = await xmlProvider.GetSchemaAsync("confirmrequestchangeofsupplier", "0.1").ConfigureAwait(false);
        Assert.Equal(typeof(XmlSchema), schema?.GetType());
    }

    [Fact]
    public async Task SomeTest2()
    {
        var xmlProvider = SchemaProviderFactory.GetProvider(MediaTypeNames.Application.Json);
        var schema = await xmlProvider.GetSchemaAsync("confirmrequestchangeofsupplier", "0.1").ConfigureAwait(false);
        Assert.Equal(typeof(XmlSchema), schema?.GetType());
    }
}
