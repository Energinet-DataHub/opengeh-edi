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

using Energinet.DataHub.Core.App.Common.Extensions.DependencyInjection;
using Energinet.DataHub.Core.App.FunctionApp.Extensions.Builder;
using Energinet.DataHub.Core.App.FunctionApp.Extensions.DependencyInjection;
using Energinet.DataHub.ProcessManagement.Core.Infrastructure.Extensions.DependencyInjection;
using Energinet.DataHub.ProcessManagement.Core.Infrastructure.Telemetry;
using Energinet.DataHub.ProcessManager.Scheduler;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        // HttpTrigger serializtion
        //
        // Based on input from the following:
        //  - https://github.com/Azure/azure-functions-dotnet-worker/issues/2131
        //  - https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide?tabs=hostbuilder%2Cwindows#json-serialization-with-aspnet-core-integration
        //
        // TODO:
        // If this works then perhaps we should implement an extension for easy use with
        // Azure Functions applications using ASP.NET Core integration.
        services
            .AddMvc()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(NodaConverters.InstantConverter);
                options.JsonSerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
            });

        // Common
        services.AddApplicationInsightsForIsolatedWorker(TelemetryConstants.SubsystemName);
        services.AddHealthChecksForIsolatedWorker();
        services.AddNodaTimeForApplication();

        // Scheduler
        services.AddScoped<SchedulerHandler>();

        // ProcessManager
        services.AddProcessManagerCore();
    })
    .ConfigureLogging((hostingContext, logging) =>
    {
        logging.AddLoggingConfigurationForIsolatedWorker(hostingContext);
    })
    .Build();

host.Run();
