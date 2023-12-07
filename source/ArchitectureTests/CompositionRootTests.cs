﻿// Copyright 2020 Energinet DataHub A/S
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
using Energinet.DataHub.EDI.Api;
using Energinet.DataHub.EDI.B2CWebApi;
using Energinet.DataHub.EDI.Infrastructure.Configuration;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.OutgoingMessages.Application.MarketDocuments.AggregationResult;
using Energinet.DataHub.EDI.OutgoingMessages.Application.MarketDocuments.Xml;
using Energinet.DataHub.EDI.OutgoingMessages.Application.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.MarketDocuments;
using Energinet.DataHub.EDI.Process.Application.Transactions.AggregatedMeasureData;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
using Program = Energinet.DataHub.EDI.Api.Program;

namespace Energinet.DataHub.EDI.ArchitectureTests
{
    public class CompositionRootTests
    {
        private readonly IHost _host;

        public CompositionRootTests()
        {
            var testEnvironment = new TestEnvironment();
            Environment.SetEnvironmentVariable("SERVICE_BUS_CONNECTION_STRING_FOR_DOMAIN_RELAY_SEND", TestEnvironment.CreateFakeServiceBusConnectionString());
            Environment.SetEnvironmentVariable("WHOLESALE_INBOX_MESSAGE_QUEUE_NAME", "FakeQueueName");
            Environment.SetEnvironmentVariable("INCOMING_MESSAGES_QUEUE_NAME", "FakeQueueName");
            Environment.SetEnvironmentVariable("DB_CONNECTION_STRING", TestEnvironment.CreateConnectionString());
            var config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();
            _host = Program.ConfigureHost(Program.DevelopmentTokenValidationParameters(), testEnvironment, config);
        }

        #region Member data providers

        public static IEnumerable<object[]> GetDocumentWriterRequirements()
        {
            var constructorDependencies = ReflectionHelper.FindAllConstructorDependenciesForType();
            return typeof(AggregationResultXmlDocumentWriter).Assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(DocumentWriter)))
                .Select(t => new object[] { new Requirement(t.Name, constructorDependencies(t), t) });
        }

        public static IEnumerable<object[]> GetRequestHandlerRequirements()
            => ResolveTypes(
                typeof(IRequestHandler<,>),
                new[] { typeof(InitializeAggregatedMeasureDataProcessesHandler).Assembly, typeof(MessagePeeker).Assembly });

        public static IEnumerable<object[]> GetNotificationsHandlerRequirements()
            => ResolveTypes(
                typeof(INotificationHandler<>),
                new[] { ApplicationAssemblies.Application, ApplicationAssemblies.Infrastructure });

        public static IEnumerable<object[]> GetDocumentWritersRequirements()
            => ResolveTypesThatImplementType(
                typeof(IDocumentWriter),
                new[] { ApplicationAssemblies.Application, ApplicationAssemblies.Infrastructure, typeof(AggregationResultXmlDocumentWriter).Assembly });

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
            Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseDefaultServiceProvider((_, options) =>
                        {
                            // See https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/web-host?view=aspnetcore-7.0#scope-validation
                            options.ValidateScopes = true;
                            // Validate the service provider during build
                            options.ValidateOnBuild = true;
                        })
                        // Add controllers as services to enable validation of controller dependencies
                        // See https://andrewlock.net/new-in-asp-net-core-3-service-provider-validation/#1-controller-constructor-dependencies-aren-t-checked
                        .ConfigureServices(collection => collection.AddControllers().AddControllersAsServices())
                        .UseStartup<Startup>();
                }).Build();
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
            public override string? SERVICE_BUS_CONNECTION_STRING_FOR_DOMAIN_RELAY_SEND =>
                CreateFakeServiceBusConnectionString();

            public override string? REQUEST_RESPONSE_LOGGING_CONNECTION_STRING =>
                CreateFakeServiceBusConnectionString();

            public override string? DB_CONNECTION_STRING =>
                CreateConnectionString();

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
