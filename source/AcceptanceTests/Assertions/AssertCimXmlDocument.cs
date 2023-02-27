using System.Xml.Linq;

namespace AcceptanceTest.Assertions;

public class AssertCimXmlDocument
{
    private readonly XDocument _document;

    public AssertCimXmlDocument(Stream body)
    {
        _document = XDocument.Load(body, LoadOptions.None);
    }

    public void IsProductionResultFor(string expectedGridArea)
    {
        var series = _document.Root?.Elements().Where(e => e.Name.LocalName.Equals("Series", StringComparison.Ordinal)).ToList();
        var marketEvaluationPointType = series!.Elements()
            .Single(e => e.Name.LocalName.Equals("marketEvaluationPoint.type", StringComparison.OrdinalIgnoreCase))
            .Value;
        var gridArea = series!.Elements()
            .Single(e => e.Name.LocalName.Equals("meteringGridArea_Domain.mRID", StringComparison.OrdinalIgnoreCase))
            .Value;
        Assert.Equal("E18", marketEvaluationPointType);
        var documentType = _document.Root?.Name.LocalName;
        Assert.Equal(expectedGridArea, gridArea);
        Assert.Equal("NotifyAggregatedMeasureData_MarketDocument", documentType);
    }
}
