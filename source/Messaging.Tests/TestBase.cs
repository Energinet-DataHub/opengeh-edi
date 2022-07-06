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

using System;
using Energinet.DataHub.Core.App.FunctionApp.Middleware.CorrelationId;
using Messaging.Infrastructure.Configuration;
using Messaging.Infrastructure.Transactions.MoveIn;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Messaging.Tests;

[Collection("Test")]
public class TestBase : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private bool _disposed;

    protected TestBase()
    {
        var services = new ServiceCollection();
        CompositionRoot.Initialize(services)
            .AddMessageParserServices();
        _serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected T GetService<T>()
        where T : notnull
    {
        return _serviceProvider.GetRequiredService<T>();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed == true)
        {
            return;
        }

        ((ServiceProvider)_serviceProvider).Dispose();
        _disposed = true;
    }
}
