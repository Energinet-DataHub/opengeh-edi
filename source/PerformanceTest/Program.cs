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

using PerformanceTest.Actors;
using PerformanceTest.MoveIn;

namespace PerformanceTest;

public sealed class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
        builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddSwaggerGen();

        builder.Services.AddSingleton<IActorService, ActorService>();

        builder.Services.AddTransient<IMoveInService, MoveInService>();

        builder.Services.AddSingleton(builder.Configuration);

        builder.Services.AddHealthChecks();

        builder.Services.AddLogging();

// CONCLUSION:
//  * Logging using ILogger<T> will work, but notice that by default we need to log as "Warning" for it to appear in Application Insights (can be configured).
//    See "How do I customize ILogger logs collection" at https://docs.microsoft.com/en-us/azure/azure-monitor/faq#how-do-i-customize-ilogger-logs-collection-

// CONCLUSION:
//  * We can see Trace, Request, Dependencies and other entries in App Insights out-of-box.
//    See https://docs.microsoft.com/en-us/azure/azure-monitor/app/asp-net-core
        builder.Services.AddApplicationInsightsTelemetry();

        var app = builder.Build();

// Configure the HTTP request pipeline.
        app.UseSwagger();

        app.UseSwaggerUI();

        app.UseAuthorization();

        app.MapControllers();

        app.MapHealthChecks("monitor/live");

        app.MapHealthChecks("monitor/ready");

        app.Run();
    }
}
