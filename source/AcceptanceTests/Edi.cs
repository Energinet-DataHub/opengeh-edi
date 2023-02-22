using System.Net;
using System.Net.Http.Headers;
using AcceptanceTest.Factories;

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

    public async Task AssertTotalProductionResultIsAvailableAsync(string gridOperatorNumber, DocumentFormat documentFormat)
    {
        var peekResponse = await _driver.PeekAsync(TokenBuilder.ForGridOperator(gridOperatorNumber)).ConfigureAwait(false);

        Assert.Equal(HttpStatusCode.OK, peekResponse.StatusCode);
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

internal class EdiDriver : IDisposable
{
    private readonly HttpClient _httpClient;

    public EdiDriver()
    {
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri("http://localhost:7071/api/");
    }

    public async Task<HttpResponseMessage> PeekAsync(string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "peek/aggregations");
        request.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);
        var peekResponse = await _httpClient.SendAsync(request).ConfigureAwait(false);
        peekResponse.EnsureSuccessStatusCode();
        return peekResponse;
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
