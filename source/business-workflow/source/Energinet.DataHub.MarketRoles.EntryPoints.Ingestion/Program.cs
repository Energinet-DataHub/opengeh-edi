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
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Energinet.DataHub.MarketRoles.EntryPoints.Ingestion
{
    public static class Program
    {
        public static async Task Main()
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices(
                    serviceCollection =>
                    {
                        serviceCollection.AddMediatR(typeof(RequestChangeOfSupplier).Assembly);
                        serviceCollection.AddScoped<IPipelineBehavior<RequestChangeOfSupplier, RequestChangeOfSupplierResult>, InputValidationBehavior>();
                        serviceCollection.AddScoped<IPipelineBehavior<RequestChangeOfSupplier, RequestChangeOfSupplierResult>, AuthorizationBehavior>();
                    })
                .Build();

            await host.RunAsync().ConfigureAwait(false);
        }
    }
}
