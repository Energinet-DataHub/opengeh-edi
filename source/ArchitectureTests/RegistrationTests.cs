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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using BuildingBlocks.Application.Extensions.Options;
using Energinet.DataHub.Core.App.WebApp.Extensions.Options;
using Energinet.DataHub.EDI.B2BApi;
using Energinet.DataHub.EDI.B2BApi.DataRetention;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Configuration.Options;
using Energinet.DataHub.EDI.OutgoingMessages.Application;
using Energinet.DataHub.EDI.OutgoingMessages.Application.UseCases;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.NotifyAggregatedMeasureData;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.NotifyWholesaleServices;
using Energinet.DataHub.EDI.Process.Application.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.Options;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Energinet.DataHub.EDI.ArchitectureTests
{
    public class RegistrationTests
    {
        private readonly IHost _host;

        public RegistrationTests()
        {
            Environment.SetEnvironmentVariable($"{IncomingMessagesQueueOptions.SectionName}__{nameof(IncomingMessagesQueueOptions.QueueName)}", "FakeQueueNameIncoming");
            Environment.SetEnvironmentVariable($"{WholesaleInboxOptions.SectionName}__{nameof(WholesaleInboxOptions.QueueName)}", "FakeQueueNameWholesale");
            Environment.SetEnvironmentVariable($"{EdiInboxOptions.SectionName}__{nameof(EdiInboxOptions.QueueName)}", "FakeQueueNameEdi");
            Environment.SetEnvironmentVariable("DB_CONNECTION_STRING", TestEnvironment.CreateConnectionString());
            Environment.SetEnvironmentVariable("AZURE_STORAGE_ACCOUNT_CONNECTION_STRING", TestEnvironment.CreateDevelopmentStorageConnectionString());

            // The following declaration slows down the test execution, since create a new Uri us a heavy operation
            Environment.SetEnvironmentVariable("AZURE_STORAGE_ACCOUNT_URL", TestEnvironment.CreateFakeStorageUrl());

            Environment.SetEnvironmentVariable($"{ServiceBusOptions.SectionName}__{nameof(ServiceBusOptions.ListenConnectionString)}", TestEnvironment.CreateFakeServiceBusConnectionString());
            Environment.SetEnvironmentVariable($"{ServiceBusOptions.SectionName}__{nameof(ServiceBusOptions.SendConnectionString)}", TestEnvironment.CreateFakeServiceBusConnectionString());

            _host = HostFactory.CreateHost(Program.TokenValidationParameters);
        }

        #region Member data providers

        public static IEnumerable<object[]> GetDocumentWriterRequirements()
        {
            var constructorDependencies = ReflectionHelper.FindAllConstructorDependenciesForType();
            return typeof(NotifyAggregatedMeasureDataCimXmlDocumentWriter).Assembly.GetTypes()
                .Where(t => t.GetInterfaces().Contains(typeof(IDocumentWriter)) && !t.IsAbstract)
                .Select(t => new object[] { new Requirement(t.Name, constructorDependencies(t), t) });
        }

        public static IEnumerable<object[]> GetRequestHandlerRequirements()
            => ResolveTypes(
                typeof(IRequestHandler<,>),
                new[] { typeof(InitializeAggregatedMeasureDataProcessesHandler).Assembly, typeof(PeekMessage).Assembly });

        public static IEnumerable<object[]> GetNotificationsHandlerRequirements()
            => ResolveTypes(
                typeof(INotificationHandler<>),
                new[]
                {
                    typeof(ExecuteDataRetentionsWhenADayHasPassed).Assembly,
                    typeof(Process.Application.Transactions.AggregatedMeasureData.Notifications.Handlers.EnqueueAcceptedEnergyResultMessageHandler).Assembly,
                    typeof(Process.Infrastructure.InboxEvents.ProcessInboxEventsOnTenSecondsHasPassed).Assembly,
                });

        public static IEnumerable<object[]> GetDocumentWritersRequirements()
            => ResolveTypesThatImplementType(
                typeof(IDocumentWriter),
                new[]
                {
                    typeof(NotifyWholesaleServicesEbixDocumentWriter).Assembly,
                });

        public static IEnumerable<object[]> GetFunctionRequirements()
        {
            var allTypes = ReflectionHelper.FindAllTypes();
            var functionTypes = ReflectionHelper.FindAllFunctionTypes();
            var constructorDependencies = ReflectionHelper.FindAllConstructorDependenciesForType();

            return functionTypes(allTypes(typeof(Program)))
                .Select(f => new object[] { new Requirement(f.Name, constructorDependencies(f)) });
        }

        public static IEnumerable<object[]> GetMiddlewareRequirements()
        {
            var allTypes = ReflectionHelper.FindAllTypes();
            var middlewareTypes = ReflectionHelper.FindAllTypesThatImplementType();
            var constructorDependencies = ReflectionHelper.FindAllConstructorDependenciesForType();

            return
                middlewareTypes(typeof(IFunctionsWorkerMiddleware), allTypes(typeof(Program)))
                    .Select(m => new object[] { new Requirement(m.Name, constructorDependencies(m)) });
        }

        #endregion

        [Theory(DisplayName = nameof(All_document_writers_are_satisfied))]
        [MemberData(nameof(GetDocumentWriterRequirements))]
        public void All_document_writers_are_satisfied(Requirement requirement)
        {
            using var scope = _host.Services.CreateScope();
            Assert.True(scope.ServiceProvider.CanSatisfyRequirement(requirement));
            Assert.True(scope.ServiceProvider.RequirementIsPartOfCollection<IDocumentWriter>(requirement));
        }

        [Theory(DisplayName = nameof(All_request_handlers_are_registered))]
        [MemberData(nameof(GetRequestHandlerRequirements))]
        public void All_request_handlers_are_registered(Requirement requirement)
        {
            using var scope = _host.Services.CreateScope();
            Assert.True(scope.ServiceProvider.CanSatisfyRequirement(requirement));
        }

        [Theory(DisplayName = nameof(All_notification_handlers_are_registered))]
        [MemberData(nameof(GetNotificationsHandlerRequirements))]
        public void All_notification_handlers_are_registered(Requirement requirement)
        {
            using var scope = _host.Services.CreateScope();
            Assert.True(scope.ServiceProvider.CanSatisfyRequirement(requirement));
        }

        [Theory(DisplayName = nameof(All_document_writers_are_registered))]
        [MemberData(nameof(GetDocumentWritersRequirements))]
        public void All_document_writers_are_registered(Requirement requirement)
        {
            using var scope = _host.Services.CreateScope();
            Assert.True(scope.ServiceProvider.CanSatisfyType(requirement));
        }

        [Theory(DisplayName = nameof(All_dependencies_can_be_resolved_for_middleware))]
        [MemberData(nameof(GetMiddlewareRequirements))]
        public void All_dependencies_can_be_resolved_for_middleware(Requirement requirement)
        {
            using var scope = _host.Services.CreateScope();
            Assert.True(scope.ServiceProvider.CanSatisfyRequirement(requirement));
        }

        [Theory(DisplayName = nameof(All_dependencies_can_be_resolved_in_functions))]
        [MemberData(nameof(GetFunctionRequirements))]
        public void All_dependencies_can_be_resolved_in_functions(Requirement requirement)
        {
            using var scope = _host.Services.CreateScope();
            Assert.True(scope.ServiceProvider.CanSatisfyRequirement(requirement));
        }

        [Fact(DisplayName = nameof(All_dependencies_can_be_resolved_in_b2c_app))]
        public void All_dependencies_can_be_resolved_in_b2c_app()
        {
            var testConfiguration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        [$"{ServiceBusOptions.SectionName}__{nameof(ServiceBusOptions.ListenConnectionString)}"] = "Fake",
                        [$"{ServiceBusOptions.SectionName}__{nameof(ServiceBusOptions.SendConnectionString)}"] = "Fake",

                        [$"{UserAuthenticationOptions.SectionName}:{nameof(UserAuthenticationOptions.MitIdExternalMetadataAddress)}"] = "NotEmpty",
                        [$"{UserAuthenticationOptions.SectionName}:{nameof(UserAuthenticationOptions.ExternalMetadataAddress)}"] = "NotEmpty",
                        [$"{UserAuthenticationOptions.SectionName}:{nameof(UserAuthenticationOptions.BackendBffAppId)}"] = "NotEmpty",
                        [$"{UserAuthenticationOptions.SectionName}:{nameof(UserAuthenticationOptions.InternalMetadataAddress)}"] = "NotEmpty",
                    })
                .Build();

#pragma warning disable CA2000
            using var application = new WebApplicationFactory<global::Energinet.DataHub.EDI.B2CWebApi.Program>()
#pragma warning restore CA2000
                .WithWebHostBuilder(
                    webBuilder =>
                    {
                        webBuilder.UseConfiguration(testConfiguration);
                        webBuilder.UseDefaultServiceProvider(
                            (_, options) =>
                            {
                                // See https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/web-host?view=aspnetcore-7.0#scope-validation
                                options.ValidateScopes = true;
                                // Validate the service provider during build
                                options.ValidateOnBuild = true;
                            })
                            // Add controllers as services to enable validation of controller dependencies
                            // See https://andrewlock.net/new-in-asp-net-core-3-service-provider-validation/#1-controller-constructor-dependencies-aren-t-checked
                            .ConfigureServices(services =>
                            {
                                services.AddControllers().AddControllersAsServices();
                            });
                    })
                .CreateClient(); // This will resolve the dependency injections, hence the test
        }

        [Fact]
        public void All_middlewares_in_functions_should_be_registered()
        {
            var allTypes = ReflectionHelper.FindAllTypes();
            var middlewareTypes = ReflectionHelper.FindAllTypesThatImplementType();
            var middlewares = middlewareTypes(typeof(IFunctionsWorkerMiddleware), allTypes(typeof(Program)));
            middlewares.Should()
                .AllSatisfy(
                    middleware =>
                        _host.Services.GetService(middleware).Should().NotBeNull());
        }

        private static IEnumerable<object[]> ResolveTypes(Type targetType, Assembly[] assemblies)
        {
            var allTypes = ReflectionHelper.FindAllTypesInAssemblies();
            var allImplementations = ReflectionHelper.FindAllTypesThatImplementGenericInterface();
            var getGenericTypeDefinition = ReflectionHelper.MapToUnderlyingType();

            return allImplementations(targetType, allTypes(assemblies))
                .Select(type => new object[] { new Requirement(type.Name, getGenericTypeDefinition(type, targetType)) });
        }

        private static IEnumerable<object[]> ResolveTypesThatImplementType(Type targetType, Assembly[] assemblies)
        {
            var allTypes = ReflectionHelper.FindAllTypesInAssemblies();
            var allImplementations = ReflectionHelper.FindAllTypesThatImplementType();

            return allImplementations(targetType, allTypes(assemblies))
                .Select(type => new object[] { new Requirement(type.Name, new List<Type> { targetType }, type) });
        }

        private sealed class TestEnvironment : RuntimeEnvironment
        {
            public override string? ServiceBus__SendConnectionString =>
                CreateFakeServiceBusConnectionString();

            public override string? REQUEST_RESPONSE_LOGGING_CONNECTION_STRING =>
                CreateFakeServiceBusConnectionString();

            public override string? DB_CONNECTION_STRING =>
                CreateConnectionString();

            public override Uri? AZURE_STORAGE_ACCOUNT_URL => new Uri(CreateFakeStorageUrl());

            public override string AZURE_FUNCTIONS_ENVIRONMENT => "Development";

            public static string CreateFakeServiceBusConnectionString()
            {
                return new StringBuilder()
                    .Append(CultureInfo.InvariantCulture, $"Endpoint=sb://sb-{Guid.NewGuid():N}.servicebus.windows.net/;")
                    .Append("SharedAccessKeyName=send;")
                    .Append(CultureInfo.InvariantCulture, $"SharedAccessKey={Guid.NewGuid():N}")
                    .ToString();
            }

            public static string CreateConnectionString()
            {
                return
                    "Server=(LocalDB)\\\\MSSQLLocalDB;Database=B2BTransactions;User=User;Password=Password;TrustServerCertificate=true;Encrypt=True;Trusted_Connection=True;";
            }

            public static string CreateFakeStorageUrl() => "https://dummy.url";

            public static string CreateDevelopmentStorageConnectionString() => "UseDevelopmentStorage=true";

            public override bool IsRunningLocally()
            {
                return true;
            }

            protected override string? GetEnvironmentVariable(string variable)
            {
                return Guid.NewGuid().ToString();
            }
        }
    }
}
