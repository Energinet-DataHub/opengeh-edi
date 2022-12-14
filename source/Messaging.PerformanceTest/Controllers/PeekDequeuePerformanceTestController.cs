using Messaging.PerformanceTest.Actors;
using Messaging.PerformanceTest.MoveIn;
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
