using AcceptanceTest.Fixtures;
using Xunit.Abstractions;
using Xunit.Categories;

namespace AcceptanceTest;

[IntegrationTest]
[Collection(nameof(TestCommonCollectionFixture))]
public sealed class WhenAnAggregationsHasCompletedTests : IAsyncLifetime, IDisposable
{
    private readonly WholeSaleService _wholeSaleService;
    private readonly EdiService _ediService;

    private readonly TestCommonHostFixture _fixture;

    public WhenAnAggregationsHasCompletedTests(TestCommonHostFixture fixture, ITestOutputHelper testOutputHelper)
    {
        _wholeSaleService = new WholeSaleService();
        _ediService = new EdiService();
        _fixture = fixture;
        _fixture.SetTestOutputHelper(testOutputHelper);
        _fixture.App01HostManager.ClearHostLog();
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _fixture.SetTestOutputHelper(null!);
        return Task.CompletedTask;
    }

    [Theory]
    [InlineData(DocumentFormat.CimXml)]
    public async Task GridOperator_can_fetch_the_result_for_total_production(DocumentFormat documentFormat)
    {
        var gridArea = "543";
        await _wholeSaleService.AggregationProcessHasCompletedForAsync(gridArea).ConfigureAwait(false);
        await _ediService.AssertTotalProductionResultIsAvailableAsync("5790000610976", gridArea, documentFormat).ConfigureAwait(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            _ediService.Dispose();
        }
    }
}

#pragma warning disable

public enum DocumentFormat
{
    CimXml,
}
