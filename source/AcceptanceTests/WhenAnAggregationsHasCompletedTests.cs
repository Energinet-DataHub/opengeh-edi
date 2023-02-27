using AcceptanceTest.WholesaleApiMock;

namespace AcceptanceTest;

public class WhenAnAggregationsHasCompletedTests : IDisposable
{
    private readonly WholeSaleApiMockHost _wholeSaleApiMockHost;
    private readonly WholeSaleDsl _wholeSaleDsl;
    private readonly Edi _edi;

    public WhenAnAggregationsHasCompletedTests()
    {
        _wholeSaleApiMockHost = new WholeSaleApiMockHost();
        _wholeSaleDsl = new WholeSaleDsl();
        _edi = new Edi();
    }

    [Theory]
    [InlineData(DocumentFormat.CimXml)]
    public async Task GridOperator_can_fetch_the_result_for_total_production(DocumentFormat documentFormat)
    {
        var gridArea = "543";
        await _wholeSaleDsl.AggregationProcessHasCompletedForAsync(gridArea).ConfigureAwait(false);
        await _edi.AssertTotalProductionResultIsAvailableAsync("5790000610976", gridArea, documentFormat).ConfigureAwait(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _wholeSaleApiMockHost.Dispose();
            _edi.Dispose();
        }
    }
}

#pragma warning disable

public enum DocumentFormat
{
    CimXml,
}
