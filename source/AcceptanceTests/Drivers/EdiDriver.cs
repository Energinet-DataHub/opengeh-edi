using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using AcceptanceTest.Factories;

namespace AcceptanceTest.Drivers;

internal class EdiDriver : IDisposable
{
    private readonly HttpClient _httpClient;

    public EdiDriver()
    {
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri("http://localhost:7071/api/");
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    public async Task<Stream> PeekMessageAsync(string gridOperatorNumber)
    {
        var token = TokenBuilder.ForGridOperator(gridOperatorNumber);
        var stopWatch = Stopwatch.StartNew();
        while (stopWatch.ElapsedMilliseconds < 20000)
        {
            var peekResponse = await PeekAsync(token)
                .ConfigureAwait(false);
            if (peekResponse.StatusCode == HttpStatusCode.OK)
            {
                var document = await peekResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);
                await DequeueAsync(token, GetMessageId(peekResponse)).ConfigureAwait(false);
                return document;
            }
        }

        throw new TimeoutException("Unable to retrieve peek result within time limit");
    }

    private static string GetMessageId(HttpResponseMessage peekResponse)
    {
        return peekResponse.Headers.GetValues("MessageId").First();
    }

    private async Task<HttpResponseMessage> PeekAsync(string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "peek/aggregations");
        request.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);
        var peekResponse = await _httpClient.SendAsync(request).ConfigureAwait(false);
        peekResponse.EnsureSuccessStatusCode();
        return peekResponse;
    }

    private async Task DequeueAsync(string token, string messageId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"dequeue/{messageId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);
        var dequeueResponse = await _httpClient.SendAsync(request).ConfigureAwait(false);
        dequeueResponse.EnsureSuccessStatusCode();
    }
}
