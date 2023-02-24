using AcceptanceTest.Assertions;
using AcceptanceTest.Drivers;

namespace AcceptanceTest;

public class Edi : IDisposable
{
    private readonly EdiDriver _driver;

    public Edi()
    {
        _driver = new EdiDriver();
    }

    ~Edi()
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
