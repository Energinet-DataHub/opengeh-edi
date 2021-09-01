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
using System.Threading.Tasks;
using Energinet.DataHub.MarketRoles.EntryPoints.Common.SimpleInjector;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using SimpleInjector;

namespace Energinet.DataHub.MarketRoles.EntryPoints.Common
{
    public abstract class EntryPoint
    {
        private readonly Container _container;

        protected EntryPoint()
            : this(new Container()) { }

        private EntryPoint(Container container)
        {
            _container = container;
        }

        public IHost ConfigureApplication()
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults(ConfigureFunctionsWorkerDefaults)
                .ConfigureServices(services =>
                {
                    var descriptor = new ServiceDescriptor(
                        typeof(IFunctionActivator),
                        typeof(SimpleInjectorActivator),
                        ServiceLifetime.Singleton);
                    services.Replace(descriptor); // Replace existing activator

                    // Configure IServiceCollection dependencies for specific entrypoint.
                    ConfigureServiceCollection(services);

                    services.AddApplicationInsightsTelemetryWorkerService(
                        Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY")
                        ?? throw new InvalidOperationException("Missing APPINSIGHTS_INSTRUMENTATIONKEY"));

                    services.AddLogging();
                    services.AddSimpleInjector(_container, options =>
                    {
                        options.AddLogging();
                    });
                })
                .Build()
                .UseSimpleInjector(_container);

            // Configure regular dependencies for specific entrypoint.
            ConfigureContainer(_container);

            return host;
        }

        public void AssertConfiguration() => _container.Verify();

        public async Task ExecuteApplicationAsync(IHost host)
        {
            await host.RunAsync().ConfigureAwait(false);
            await _container.DisposeContainerAsync().ConfigureAwait(false);
        }

        protected virtual void ConfigureFunctionsWorkerDefaults(IFunctionsWorkerApplicationBuilder options)
        {
            options.UseMiddleware<SimpleInjectorScopedRequest>();
        }

        protected virtual void ConfigureServiceCollection(IServiceCollection services) { }

        protected virtual void ConfigureContainer(Container container) { }
    }
}
