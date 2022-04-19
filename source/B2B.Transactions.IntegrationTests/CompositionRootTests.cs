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
using System.Linq;
using System.Text;
using B2B.Transactions.Api;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace B2B.Transactions.IntegrationTests
{
    public class CompositionRootTests
    {
        [Fact]
        public void CompositionRoot_WithConfiguredDependencies_AllFunctionDependenciesCanBeResolved()
        {
            var host = Program.ConfigureHost(Program.DevelopmentTokenValidationParameters(), new TestEnvironment());

            var allTypes = FunctionsReflectionHelper.FindAllTypes();
            var functionTypes = FunctionsReflectionHelper.FindAllFunctionTypes();
            var constructorDependencies = FunctionsReflectionHelper.FindAllConstructorDependencies();

            var dependencies = constructorDependencies(functionTypes(allTypes(typeof(Program))));

            using var scope = host.Services.CreateScope();

            foreach (var dependency in dependencies)
            {
                var resolvedInstance = scope.ServiceProvider.GetService(dependency);
                Assert.True(resolvedInstance != null, $"Can resolve {dependency.Name}");
            }
        }

        private class TestEnvironment : RuntimeEnvironment
        {
            public override string? TRANSACTIONS_QUEUE_SENDER_CONNECTION_STRING =>
                CreateFakeServiceBusConnectionString();

            public override string? REQUEST_RESPONSE_LOGGING_CONNECTION_STRING =>
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
                var sb = new StringBuilder();
                sb.Append($"Endpoint=sb://sb-{Guid.NewGuid():N}.servicebus.windows.net/;");
                sb.Append("SharedAccessKeyName=send;");
                sb.Append($"SharedAccessKey={Guid.NewGuid():N}");
                return sb.ToString();
            }
        }
    }
}
