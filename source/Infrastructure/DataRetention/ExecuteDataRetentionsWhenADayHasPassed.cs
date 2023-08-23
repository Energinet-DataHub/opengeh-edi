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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Configuration.TimeEvents;
using MediatR;
using Microsoft.Extensions.Logging;
using Polly;

namespace Infrastructure.DataRetention;

public class ExecuteDataRetentionsWhenADayHasPassed : INotificationHandler<ADayHasPassed>
{
    private readonly IReadOnlyCollection<IDataRetention> _dataRetentions;
    private readonly ILogger<ExecuteDataRetentionsWhenADayHasPassed> _logger;

    public ExecuteDataRetentionsWhenADayHasPassed(
        IEnumerable<IDataRetention> dataRetentions,
        ILogger<ExecuteDataRetentionsWhenADayHasPassed> logger)
    {
        _dataRetentions = dataRetentions.ToList();
        _logger = logger;
    }

    public async Task Handle(ADayHasPassed notification, CancellationToken cancellationToken)
    {
        var executionPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(new[]
            {
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(4),
            });

        foreach (var dataCleaner in _dataRetentions)
        {
            var result = await executionPolicy.ExecuteAndCaptureAsync(() =>
                dataCleaner.CleanupAsync(cancellationToken)).ConfigureAwait(false);

            if (result.Outcome == OutcomeType.Failure)
            {
                _logger?.Log(LogLevel.Error, result.FinalException, "Type {DataCleaner} failed to clean up", nameof(dataCleaner));
            }
        }
    }
}
