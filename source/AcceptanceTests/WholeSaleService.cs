using System.Net.Http.Json;

namespace AcceptanceTest;

public class WholeSaleService
{
    #pragma warning disable
    public async Task AggregationProcessHasCompletedForAsync(string gridArea)
    {
        using var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri("http://localhost:5000/api/v1.0/");
        var response = await httpClient.PostAsJsonAsync("simulateprocesscompleted", new SimulateProcessHasCompleted(gridArea)).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    private record SimulateProcessHasCompleted(string GridArea);
}
