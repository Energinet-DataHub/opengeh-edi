using System.Runtime.CompilerServices;
using Messaging.PerformanceTest.Actors;
using Messaging.PerformanceTest.MoveIn;
using Messaging.PerformanceTest.MoveIn.Jwt;
using Microsoft.AspNetCore.Mvc;

namespace Messaging.PerformanceTest.Controllers;

[ApiController]
[Route("api")]
public class PeekDequeuePerformanceTestController : ControllerBase
{
    private readonly IActorService _actorService;
    private readonly IMoveInService _moveInService;
    private bool _isDataBuildInProgress;

    public PeekDequeuePerformanceTestController(
        IActorService actorService,
        IMoveInService moveInService)
    {
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

    [HttpPut("ResetActors", Name = "ResetActors")]
    public void ResetActors()
    {
        _actorService.ResetActorNumbers();
    }

    [HttpPost("GenerateTestData", Name = "GenerateTestData")]
    public async Task PostAsync()
    {
        if (_isDataBuildInProgress is false)
        {
            _isDataBuildInProgress = true;
            var actors = _actorService.GetActors();

        var tasks = new List<Task>(_actorService.GetActorCount());
            for (var j = 0; j < 1000; j++)
            {
                tasks.Clear();
                tasks.AddRange(actors.Select(actorNumber => _moveInService.MoveInAsync(actorNumber)));
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }

            _isDataBuildInProgress = false;
        }
    }
}
