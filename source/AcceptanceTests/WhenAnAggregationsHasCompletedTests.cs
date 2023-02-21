namespace AcceptanceTest;

public class WhenAnAggregationsHasCompletedTests
{
    private readonly WholeSale _wholeSale;
    private readonly Edi _edi;

    public WhenAnAggregationsHasCompletedTests()
    {
        _wholeSale = new WholeSale();
        _edi = new Edi();
    }

    [Theory]
    [InlineData(DocumentFormat.CimXml)]
    public void GridOperator_can_fetch_the_result_for_total_production(DocumentFormat documentFormat)
    {
        _wholeSale.AggregationProcessHasCompletedFor("543");
        _edi.AssertTotalProductionResultIsAvailable("5790000610976", documentFormat);
    }
}

#pragma warning disable

public enum DocumentFormat
{
    CimXml,
}
