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

using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.BuildingBlocks.Tests.Logging;

public class TestLogger<T> : ILogger<T>
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly List<ILogger> _loggers = [];

    public TestLogger(ITestOutputHelper testOutputHelper, Logger<T>? logger, LoggerSpy? loggerSpy)
    {
        _testOutputHelper = testOutputHelper;

        if (logger != null)
            _loggers.Add(logger);

        if (loggerSpy != null)
            _loggers.Add(loggerSpy);
    }

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        if (logLevel == LogLevel.Error || logLevel == LogLevel.Critical)
        {
            var logOutput = formatter(state, exception);
            _testOutputHelper.WriteLine("[{0}] {1}", logLevel.ToString().ToUpperInvariant(), logOutput);

            if (exception != null)
            {
                _testOutputHelper.WriteLine("Test logger found an exception: {0}", exception);
            }
        }

        _loggers.ForEach(l => l.Log(logLevel, eventId, state, exception, formatter));
    }
}
