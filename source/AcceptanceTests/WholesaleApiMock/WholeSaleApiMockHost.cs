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

using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AcceptanceTest.WholesaleApiMock;

public class WholeSaleApiMockHost : IDisposable
{
    private readonly IHost _host;
    private bool _disposed;

    public WholeSaleApiMockHost()
    {
        _host = Host
            .CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureServices(services =>
                {
                    services.AddControllers();
                    services.AddApiVersioning(config =>
                    {
                        config.DefaultApiVersion = ApiVersion.Default;
                        config.AssumeDefaultVersionWhenUnspecified = true;
                    });
                    services.AddSingleton(serviceProvider =>
                        new ServiceBusClient(serviceProvider.GetRequiredService<IConfiguration>().GetValue<string>("ServiceBusConnectionString")));
                    services.AddSingleton<ServiceBusSender>(serviceProvider =>
                        serviceProvider.GetRequiredService<ServiceBusClient>().CreateSender(serviceProvider.GetRequiredService<IConfiguration>().GetValue<string>("ServiceBusTopicName")));
                });
                webBuilder.ConfigureAppConfiguration(x => x.AddJsonFile("WholesaleApiMock\\appsettings.json").Build());
                webBuilder.Configure(app =>
                        app.UseRouting()
                            .UseEndpoints(opt => opt.MapControllers()))
                    .UseUrls($"http://localhost:5000", "https://localhost:5001");
            })
            .Build();

        new Thread(() => _host.Run()).Start();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _host.StopAsync().GetAwaiter();
            _host.Dispose();
        }

        _disposed = true;
    }
}
