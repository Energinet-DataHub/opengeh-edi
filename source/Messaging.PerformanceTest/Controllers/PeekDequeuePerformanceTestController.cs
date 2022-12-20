using System.Runtime.CompilerServices;
using Messaging.PerformanceTest.Actors;
using Messaging.PerformanceTest.MoveIn;
using Microsoft.AspNetCore.Mvc;

namespace Messaging.PerformanceTest.Controllers;

[ApiController]
[Route("api")]
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

    [HttpGet("ActorNumber", Name = "ActorNumber")]
    public string? GetActorNumber()
    {
        return _actorService.GetUniqueActorNumber();
    }

    [HttpGet("ActorToken/{actorNumber}", Name = "ActorToken")]
    public string? GetToken(string actorNumber)
    {
        return _actorService.IsActorNumberInUse(actorNumber) ? JwtBuilder.BuildToken(actorNumber) : null;
    }

    [HttpPost("GenerateTestData", Name = "GenerateTestData")]
    public async Task PostAsync()
    {
        _actorService.ResetActorNumbers();

        for (var i = 0; i < _actorService.GetActorCount(); i++)
        {
              _actorService.GetUniqueActorNumber();
        }

        var actors = _actorService.GetActors();

        var tasks = new List<Task>();
        for (var j = 0; j < 1000; j++)
        {
            tasks.Clear();
            tasks.AddRange(actors.Select(actorNumber => _moveInService.MoveInAsync(actorNumber)));
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        _actorService.ResetActorNumbers();
    }
}
