using AcceptanceTest.Assertions;
using AcceptanceTest.Drivers;

namespace AcceptanceTest;

public class EdiService : IDisposable
{
    private readonly EdiDriver _driver;

    public EdiService()
    {
        _driver = new EdiDriver();
    }

    ~EdiService()
    {
        Dispose(false);
    }

    public async Task AssertTotalProductionResultIsAvailableAsync(
        string gridOperatorNumber,
        string gridArea,
        DocumentFormat documentFormat)
    {
        var document = await _driver.PeekMessageAsync(gridOperatorNumber).ConfigureAwait(false);
        var assertDocument = new AssertCimXmlDocument(document);
        assertDocument.IsProductionResultFor(gridArea);
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
            _driver.Dispose();
        }
    }
}
