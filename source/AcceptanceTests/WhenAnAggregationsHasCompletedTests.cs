namespace AcceptanceTest;

public class WhenAnAggregationsHasCompletedTests
{
    private readonly Aggregations _aggregations;

    public WhenAnAggregationsHasCompletedTests()
    {
        _aggregations = new Aggregations();
    }

    [Theory]
    [InlineData(DocumentFormat.CimXml)]
    public void GridOperator_can_fetch_the_result_for_total_production(DocumentFormat documentFormat)
    {
        _aggregations.AggregationProcessHasCompletedFor("543");
        _aggregations.AssertTotalProductionResultIsAvailable("5790000610976", documentFormat);
    }
}

public class Aggregations
{
    #pragma warning disable
    public void AssertTotalProductionResultIsAvailable(string gridOperatorNumber, DocumentFormat documentFormat)
    {
    }

    public void AggregationProcessHasCompletedFor(string gridArea)
    {
    }
}

public enum DocumentFormat
{
    CimXml,
}
