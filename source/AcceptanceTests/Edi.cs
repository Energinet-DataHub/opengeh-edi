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
        var token = TokenBuilder.ForGridOperator(gridOperatorNumber);
        while (stopWatch.ElapsedMilliseconds < 20000)
        {
            var peekResponse = await _driver.PeekAsync(token)
                .ConfigureAwait(false);
            if (peekResponse.StatusCode == HttpStatusCode.OK)
            {
                var body = await peekResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);
                var document = await XDocument.LoadAsync(body, LoadOptions.None, CancellationToken.None).ConfigureAwait(false);
                var series = document.Root?.Elements().Where(e => e.Name.LocalName.Equals("Series", StringComparison.Ordinal));
                var marketEvaluationPointType = series!.Elements()
                    .Single(e => e.Name.LocalName.Equals("marketEvaluationPoint.type", StringComparison.OrdinalIgnoreCase))
                    .Value;
                Assert.Equal("E18", marketEvaluationPointType);
                var documentType = document.Root?.Name.LocalName;
                Assert.Equal("NotifyAggregatedMeasureData_MarketDocument", documentType);
                var messageId = GetMessageId(peekResponse);
                await _driver.DequeueAsync(token, messageId).ConfigureAwait(false);
                break;
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

    private static string GetMessageId(HttpResponseMessage peekResponse)
    {
        return peekResponse.Headers.GetValues("MessageId").First();
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

    public async Task<HttpResponseMessage> DequeueAsync(string token, string messageId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"dequeue/{messageId}");
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
