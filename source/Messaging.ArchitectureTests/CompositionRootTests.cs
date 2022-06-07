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
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using MediatR;
using Messaging.Api;
using Messaging.Infrastructure.Configuration;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Messaging.ArchitectureTests
{
    public class CompositionRootTests
    {
        private readonly IHost _host;

        public CompositionRootTests()
        {
            _host = Program.ConfigureHost(Program.DevelopmentTokenValidationParameters(), new TestEnvironment());
        }

        [Fact]
        public void All_dependencies_can_be_resolved_in_functions()
        {
            var allTypes = FunctionsReflectionHelper.FindAllTypes();
            var functionTypes = FunctionsReflectionHelper.FindAllFunctionTypes();
            var constructorDependencies = FunctionsReflectionHelper.FindAllConstructorDependencies();

            var dependencies = constructorDependencies(functionTypes(allTypes(typeof(Program))));

            using var scope = _host.Services.CreateScope();

            foreach (var dependency in dependencies)
            {
                var resolvedInstance = scope.ServiceProvider.GetService(dependency);
                Assert.True(resolvedInstance != null, $"Unable to resolve {dependency.Name}");
            }
        }

        [Fact]
        public void All_dependencies_can_be_resolved_for_middleware()
        {
            var allTypes = FunctionsReflectionHelper.FindAllTypes();
            var middlewareTypes = FunctionsReflectionHelper.FindAllTypesThatImplementType();
            var constructorDependencies = FunctionsReflectionHelper.FindAllConstructorDependencies();

            var dependencies = constructorDependencies(middlewareTypes(typeof(IFunctionsWorkerMiddleware), allTypes(typeof(Program))));
            using var scope = _host.Services.CreateScope();

            foreach (var dependency in dependencies)
            {
                var resolvedInstance = scope.ServiceProvider.GetService(dependency);
                Assert.True(resolvedInstance != null, $"Unable to resolve {dependency.Name}");
            }
        }

        [Fact]
        public void All_request_handlers_are_registered()
        {
            var assemblies = new[] { ApplicationAssemblies.Application, ApplicationAssemblies.Infrastructure, };
            var typeToLookFor = typeof(IRequestHandler<,>);

            AssertTypeIsRegistered(typeToLookFor, assemblies);
        }

        [Fact]
        public void All_notification_handlers_are_registered()
        {
            var assemblies = new[] { ApplicationAssemblies.Application, ApplicationAssemblies.Infrastructure, };
            var typeToLookFor = typeof(INotificationHandler<>);

            AssertTypeIsRegistered(typeToLookFor, assemblies);
        }

        private void AssertTypeIsRegistered(Type typeToLookFor, Assembly[] lookInAssemblies)
        {
            foreach (var assembly in lookInAssemblies)
            {
                var requestHandlerTypes = assembly
                    .GetTypes()
                    .Where(t => t.GetInterfaces()
                        .Any(i => i.IsGenericType &&
                                  i.GetGenericTypeDefinition() == typeToLookFor))
                    .SelectMany(t => t.GetInterfaces())
                    .ToList();

                requestHandlerTypes.ForEach(t =>
                {
                    _host.Services.GetRequiredService(t);
                });
            }
        }

        private class TestEnvironment : RuntimeEnvironment
        {
            public override string? MOVE_IN_REQUEST_ENDPOINT => "https://test.dk";

            public override string? INCOMING_MESSAGE_QUEUE_SENDER_CONNECTION_STRING =>
                CreateFakeServiceBusConnectionString();

            public override string? REQUEST_RESPONSE_LOGGING_CONNECTION_STRING =>
                CreateFakeServiceBusConnectionString();

            public override string? INCOMING_MESSAGE_QUEUE_MANAGE_CONNECTION_STRING =>
                CreateFakeServiceBusConnectionString();

            public override string? MESSAGEHUB_QUEUE_CONNECTION_STRING =>
                CreateFakeServiceBusConnectionString();

            public override bool IsRunningLocally()
            {
                return true;
            }

            protected override string? GetEnvironmentVariable(string variable)
            {
                return Guid.NewGuid().ToString();
            }

            private static string CreateFakeServiceBusConnectionString()
            {
                return new StringBuilder()
                    .Append(CultureInfo.InvariantCulture, $"Endpoint=sb://sb-{Guid.NewGuid():N}.servicebus.windows.net/;")
                    .Append("SharedAccessKeyName=send;")
                    .Append(CultureInfo.InvariantCulture, $"SharedAccessKey={Guid.NewGuid():N}")
                    .ToString();
            }
        }
    }
}
