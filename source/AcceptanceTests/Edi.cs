using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Xml.Linq;
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
        var stopWatch = Stopwatch.StartNew();
        while (stopWatch.ElapsedMilliseconds < 20000)
        {
            var peekResponse = await _driver.PeekAsync(TokenBuilder.ForGridOperator(gridOperatorNumber))
                .ConfigureAwait(false);
            if (peekResponse.StatusCode == HttpStatusCode.OK)
            {
                var body = await peekResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);
                var document = await XDocument.LoadAsync(body, LoadOptions.None, CancellationToken.None).ConfigureAwait(false);
                var documentName = document.Root?.Name.LocalName;
                if (documentName == "NotifyAggregatedMeasureData_MarketDocument") break;
            }
        }
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

    public async Task<HttpResponseMessage> DequeueAsync(string token, string bundleId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"dequeue/{bundleId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);
        var dequeueResponse = await _httpClient.SendAsync(request).ConfigureAwait(false);
        dequeueResponse.EnsureSuccessStatusCode();
        return dequeueResponse;
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
