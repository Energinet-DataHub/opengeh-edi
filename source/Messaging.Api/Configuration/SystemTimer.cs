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

using System.Threading.Tasks;
using MediatR;
using Messaging.Application.Configuration;
using Messaging.Application.Configuration.TimeEvents;
using Microsoft.Azure.Functions.Worker;

namespace Messaging.Api.Configuration
{
    public class SystemTimer
    {
        private readonly IMediator _mediator;
        private readonly ISystemDateTimeProvider _systemDateTimeProvider;

        public SystemTimer(IMediator mediator, ISystemDateTimeProvider systemDateTimeProvider)
        {
            _mediator = mediator;
            _systemDateTimeProvider = systemDateTimeProvider;
        }

        [Function("TenSecondsHasPassed")]
        public Task TenSecondsHasPassedAsync([TimerTrigger("*/10 * * * * *")] TimerInfo timerTimerInfo, FunctionContext context)
        {
            return _mediator.Publish(new TenSecondsHasHasPassed(_systemDateTimeProvider.Now()));
        }

        [Function("ADayHasPassed")]
        public Task ADayHasPassedAsync([TimerTrigger("0 0 * * *")] TimerInfo timerTimerInfo, FunctionContext context)
        {
            return _mediator.Publish(new ADayHasPassed(_systemDateTimeProvider.Now()));
        }
    }
}
