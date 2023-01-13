using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace LearningTests;

public class WholesaleProcessResultsApiTests
{
    private readonly IConfigurationRoot _configuration;

    public WholesaleProcessResultsApiTests()
    {
        _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", false, false)
            .Build();
    }

    [Fact]
    public async Task Fetch_aggregated_production_per_grid_area()
    {
        using var httpClient = new HttpClient();

        var request = new ProcessStepResultRequestDto(Guid.Parse(_configuration["BatchId"]!), _configuration["GridArea"]!, ProcessStepType.AggregateProductionPerGridArea);
        var response = await httpClient.PostAsJsonAsync(new Uri(_configuration["ServiceUri"]!), request).ConfigureAwait(false);

        var result = await response.Content.ReadFromJsonAsync<ProcessStepResultDto>().ConfigureAwait(false);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
    }

    #pragma warning disable
    public record ProcessStepResultRequestDto(Guid BatchId, string GridAreaCode, ProcessStepType ProcessStepResult);

    public enum ProcessStepType
    {
        AggregateProductionPerGridArea = 25,
    }

    public record ProcessStepResultDto(
        ProcessStepMeteringPointType ProcessStepMeteringPointType,
        decimal Sum,
        decimal Min,
        decimal Max,
        TimeSeriesPointDto[] TimeSeriesPoints);

    public enum ProcessStepMeteringPointType
    {
        Production = 0,
    }

    public record TimeSeriesPointDto(
        DateTimeOffset Time,
        decimal Quantity);
}
