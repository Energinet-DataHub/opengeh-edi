﻿// Copyright 2020 Energinet DataHub A/S
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

using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.TimeEvents;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using NodaTime;

namespace Energinet.DataHub.EDI.B2BApi.Configuration;

public class SystemTimer
{
    private readonly IMediator _mediator;
    private readonly IClock _clock;

    public SystemTimer(IMediator mediator, IClock clock)
    {
        _mediator = mediator;
        _clock = clock;
    }

    [Function("TenSecondsHasPassed")]
    public Task TenSecondsHasPassedAsync([TimerTrigger("*/10 * * * * *")] TimerInfo timerTimerInfo, FunctionContext context)
    {
        return _mediator.Publish(new TenSecondsHasHasPassed(_clock.GetCurrentInstant()));
    }

    [Function("ADayHasPassed")]
    public Task ADayHasPassedAsync([TimerTrigger("0 0 10 * * *")] TimerInfo timerTimerInfo, FunctionContext context)
    {
        return _mediator.Publish(new ADayHasPassed(_clock.GetCurrentInstant()));
    }
}
