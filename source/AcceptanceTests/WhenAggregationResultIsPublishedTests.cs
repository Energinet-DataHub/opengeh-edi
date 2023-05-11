using AcceptanceTest.Drivers;
using AcceptanceTest.Dsl;
using Xunit.Categories;

namespace AcceptanceTest;

[IntegrationTest]
public sealed class WhenAggregationResultIsPublishedTests : TestRunner
{
    private readonly AggregationResultDsl _aggregations;

    public WhenAggregationResultIsPublishedTests()
    {
        _aggregations = new AggregationResultDsl(
            new EdiDriver(),
            new WholeSaleDriver(EventPublisher));
    }

    [Fact]
    public async Task Actor_can_fetch_aggregation_result()
    {
        await _aggregations.PublishResultFor(gridAreaCode: "543").ConfigureAwait(false);
        await _aggregations.ConfirmResultIsAvailableFor(actorNumber: "5790000610976", actorRole: "gridoperator").ConfigureAwait(false);
    }
}
