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

using AcceptanceTest.Drivers;
using Microsoft.Extensions.Configuration;

namespace AcceptanceTest;

public class TestRunner : IAsyncDisposable
{
    protected TestRunner()
    {
    //  var configuration = ReadConfigurationSettings();
        EventPublisher = new IntegrationEventPublisher(
            @event publisher configuration
    }

    internal IntegrationEventPublisher EventPublisher { get; }

    public async ValueTask DisposeAsync()
    {
        await EventPublisher.DisposeAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    // private static IConfigurationRoot ReadConfigurationSettings()
    // {
    //     return new ConfigurationBuilder()
    //         .SetBasePath(Directory.GetCurrentDirectory())
    //         .AddJsonFile("appsettings.json", false, false)
    //         .Build();
    // }
}
