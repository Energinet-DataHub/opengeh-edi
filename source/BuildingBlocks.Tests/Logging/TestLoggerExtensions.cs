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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Energinet.DataHub.BuildingBlocks.Tests.Logging;

public static class TestLoggerExtensions
{
    public static IServiceCollection AddTestLogger(this IServiceCollection services, ITestOutputHelper testOutputHelper)
    {
        services.AddSingleton<ITestOutputHelper>(sp => testOutputHelper);
        services.Add(ServiceDescriptor.Singleton(typeof(LoggerSpy<>), typeof(LoggerSpy<>)));
        services.Add(ServiceDescriptor.Singleton(typeof(Logger<>), typeof(Logger<>)));
        services.Add(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(TestLogger<>)));

        return services;
    }
}
