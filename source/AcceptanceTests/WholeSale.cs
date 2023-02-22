using System.Net.Http.Json;

namespace AcceptanceTest;

public class WholeSale
{
    #pragma warning disable
    public async Task AggregationProcessHasCompletedForAsync(string gridArea)
    {
        using var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri("http://localhost:5000/api/");
        var response = await httpClient.PostAsJsonAsync("simulateprocesscompleted", new SimulateProcessHasCompleted(gridArea)).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    private record SimulateProcessHasCompleted(string GridArea);
}
