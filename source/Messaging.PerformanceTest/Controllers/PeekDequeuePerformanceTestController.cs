using System.Collections.Concurrent;
using Messaging.PerformanceTest.Actors;
using Microsoft.AspNetCore.Mvc;

namespace Messaging.PerformanceTest.Controllers;

[ApiController]
[Route("[controller]")]
public class PeekDequeuePerformanceTestController : ControllerBase
{
    private readonly ILogger<PeekDequeuePerformanceTestController> _logger;
    private readonly IActorService _actorService;
    private readonly IMoveInService _moveInService;

    public PeekDequeuePerformanceTestController(
        ILogger<PeekDequeuePerformanceTestController> logger,
        IActorService actorService,
        IMoveInService moveInService)
    {
        _logger = logger;
        _actorService = actorService;
        _moveInService = moveInService;
    }

    [HttpGet(Name = "GetUniqueActorNumber")]
    public string? Get()
    {
        return _actorService.GetUniqueActorNumber();
    }

    [HttpPost(Name = "GenerateTestData")]
    public async Task PostAsync()
    {
         for (var i = 0; i < _actorService.GetActorCount(); i++)
         {
             var uniqueActorNumber = _actorService.GetUniqueActorNumber();

             for (var j = 0; j < 10; j++)
             {
                 await _moveInService.MoveInAsync(uniqueActorNumber).ConfigureAwait(false);
             }
         }
    }
}

/// <summary>
/// Service for triggering a move in.
/// </summary>
public interface IMoveInService
{
    /// <summary>
    /// Request a move in for a given actor.
    /// </summary>
    /// <param name="uniqueActorNumber"></param>
    Task MoveInAsync(string? uniqueActorNumber);
}

internal class MoveInService : IMoveInService, IDisposable
{
    private readonly HttpClient _httpClient;

    public MoveInService()
    {
        _httpClient = new HttpClient();
    }

    public async Task MoveInAsync(string? uniqueActorNumber)
    {
        await _httpClient.PostAsync(new Uri($"http://localhost:7071/api/MoveIn/{uniqueActorNumber}"), null).ConfigureAwait(false);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
