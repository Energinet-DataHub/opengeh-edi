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

using System.Collections.ObjectModel;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using PerformanceTest.Actors;
using PerformanceTest.MoveIn;
using PerformanceTest.MoveIn.Jwt;

namespace PerformanceTest.Controllers;

[ApiController]
[Route("api")]
public class PeekDequeuePerformanceTestController : ControllerBase
{
    private readonly IActorService _actorService;
    private readonly IMoveInService _moveInService;
    private readonly ILogger<PeekDequeuePerformanceTestController> _logger;
    private readonly bool _runParallel;
    private readonly int _moveInsPerActor = 2000;
    private bool _isDataBuildInProgress;

    public PeekDequeuePerformanceTestController(
        IActorService actorService,
        IMoveInService moveInService,
        IConfiguration configuration,
        ILogger<PeekDequeuePerformanceTestController> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(configuration);
        _actorService = actorService;
        _moveInService = moveInService;
        _logger = logger;
        if (bool.TryParse(configuration["RunMoveInParallel"], out var runParallelConfig))
            _runParallel = runParallelConfig;
        if (int.TryParse(configuration["MoveInsPerActor"], out var moveInsPerActorConfig))
            _moveInsPerActor = moveInsPerActorConfig;
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
            _logger.LogWarning("Start GenerateTestData");
            var tasks = new List<Task>(_actorService.GetActorCount());
            try
            {
                if (_runParallel)
                {
                    await RunParallelAsync(tasks, actors).ConfigureAwait(false);
                }
                else
                {
                    await RunSingleAsync(actors).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"An error occured during GenerateTestData. Message: {e.Message} InnerException: {e.InnerException}");
                throw;
            }

            _logger.LogWarning("GenerateTestData completed");
            _isDataBuildInProgress = false;
        }
    }

    private async Task RunSingleAsync(ReadOnlyCollection<string> actors)
    {
        for (var i = 0; i < _moveInsPerActor; i++)
        {
            foreach (var actor in actors)
            {
                await _moveInService.MoveInAsync(actor).ConfigureAwait(false);
            }
        }
    }

    private async Task RunParallelAsync(List<Task> tasks, ReadOnlyCollection<string> actors)
    {
        var loopCount = 0;
        for (var j = 0; j < _moveInsPerActor; j++)
        {
            _logger.LogWarning($"GenerateTestData loopCount: {++loopCount}");
            tasks.Clear();
            tasks.AddRange(actors.Select(actorNumber => _moveInService.MoveInAsync(actorNumber)));
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }
}
