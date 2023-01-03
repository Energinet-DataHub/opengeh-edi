// Copyright 2020 Energinet DataHub A/S
//
// Licensed under the Apache License, Version 2.0 (the "License2");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
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
            for (var j = 0; j < 2000; j++)
            {
                tasks.Clear();
                tasks.AddRange(actors.Select(actorNumber => _moveInService.MoveInAsync(actorNumber)));
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }

            _isDataBuildInProgress = false;
        }
    }
}
