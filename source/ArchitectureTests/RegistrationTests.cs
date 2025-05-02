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

using System.Globalization;
using System.Reflection;
using System.Text;
using Energinet.DataHub.Core.App.Common.Extensions.Options;
using Energinet.DataHub.Core.Databricks.SqlStatementExecution;
using Energinet.DataHub.Core.Messaging.Communication.Extensions.Options;
using Energinet.DataHub.EDI.B2BApi;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Configuration;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Configuration.Options;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.DataAccess.UnitOfWork;
using Energinet.DataHub.EDI.IncomingMessages.Domain.MessageParsers;
using Energinet.DataHub.EDI.IncomingMessages.Domain.MessageParsers.RSM017;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Configuration.Options;
using Energinet.DataHub.EDI.MasterData.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.Outbox.Infrastructure;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.NotifyAggregatedMeasureData;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.NotifyWholesaleServices;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Extensions.Options;
using Energinet.DataHub.ProcessManager.Client.Extensions.Options;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Energinet.DataHub.EDI.ArchitectureTests;

public class RegistrationTests
{
    private readonly IHost _host;

    public RegistrationTests()
    {
        Environment.SetEnvironmentVariable($"{IncomingMessagesQueueOptions.SectionName}__{nameof(IncomingMessagesQueueOptions.QueueName)}", "FakeQueueNameIncoming");
        Environment.SetEnvironmentVariable("DB_CONNECTION_STRING", TestEnvironment.CreateConnectionString());
        // The following declaration slows down the test execution, since creating a new Uri is a heavy operation
        Environment.SetEnvironmentVariable(
            $"{BlobServiceClientConnectionOptions.SectionName}__{nameof(BlobServiceClientConnectionOptions.StorageAccountUrl)}",
            TestEnvironment.CreateFakeStorageUrl());

        Environment.SetEnvironmentVariable($"{ServiceBusNamespaceOptions.SectionName}__{nameof(ServiceBusNamespaceOptions.FullyQualifiedNamespace)}", TestEnvironment.CreateFakeServiceBusFullyQualifiedNamespace());

        Environment.SetEnvironmentVariable(nameof(DatabricksSqlStatementOptions.WorkspaceUrl), "https://adb-1000.azuredatabricks.net/");
        Environment.SetEnvironmentVariable(nameof(DatabricksSqlStatementOptions.WorkspaceToken), "FakeToken");
        Environment.SetEnvironmentVariable(nameof(DatabricksSqlStatementOptions.WarehouseId), Guid.NewGuid().ToString());
        Environment.SetEnvironmentVariable($"{EdiDatabricksOptions.SectionName}__{nameof(EdiDatabricksOptions.CatalogName)}", "FakeCatalogName");
        Environment.SetEnvironmentVariable($"{nameof(DeltaTableOptions.DatabricksCatalogName)}", "FakeCatalogName");

        Environment.SetEnvironmentVariable($"{ProcessManagerServiceBusClientOptions.SectionName}__{nameof(ProcessManagerServiceBusClientOptions.StartTopicName)}", "FakeTopicName");
        Environment.SetEnvironmentVariable($"{ProcessManagerServiceBusClientOptions.SectionName}__{nameof(ProcessManagerServiceBusClientOptions.NotifyTopicName)}", "FakeTopicName");
        Environment.SetEnvironmentVariable($"{ProcessManagerServiceBusClientOptions.SectionName}__{nameof(ProcessManagerServiceBusClientOptions.Brs021ForwardMeteredDataStartTopicName)}", "FakeBrs021TopicName");
        Environment.SetEnvironmentVariable($"{ProcessManagerServiceBusClientOptions.SectionName}__{nameof(ProcessManagerServiceBusClientOptions.Brs021ForwardMeteredDataNotifyTopicName)}", "FakeBrs021TopicName");

        // Dead-letter logging
        Environment.SetEnvironmentVariable($"{BlobDeadLetterLoggerOptions.SectionName}__{nameof(BlobDeadLetterLoggerOptions.StorageAccountUrl)}", TestEnvironment.CreateFakeStorageUrl());
        Environment.SetEnvironmentVariable($"{BlobDeadLetterLoggerOptions.SectionName}__{nameof(BlobDeadLetterLoggerOptions.ContainerName)}", "fake-container-name");

        Environment.SetEnvironmentVariable($"{AzureAppConfigurationOptions.SectionName}__{nameof(AzureAppConfigurationOptions.Endpoint)}", "https://appcs-intgra-t-we-002.azconfig.io");

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

    public static IEnumerable<object[]> GetDocumentWritersRequirements()
    {
        return ResolveTypesThatImplementType(
                typeof(IDocumentWriter),
                new[]
                {
                typeof(NotifyWholesaleServicesEbixDocumentWriter).Assembly,
                });
    }

    public static IEnumerable<object[]> GetMessageParserRequirements()
    {
        return ResolveTypesThatImplementType(
                typeof(IMessageParser),
                new[]
                {
                typeof(WholesaleSettlementJsonMessageParser).Assembly,
                });
    }

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

    [Theory(DisplayName = nameof(All_document_writers_are_registered))]
    [MemberData(nameof(GetDocumentWritersRequirements))]
    public void All_document_writers_are_registered(Requirement requirement)
    {
        using var scope = _host.Services.CreateScope();
        Assert.True(scope.ServiceProvider.CanSatisfyType(requirement));
    }

    [Theory(DisplayName = nameof(All_message_parsers_are_registered))]
    [MemberData(nameof(GetMessageParserRequirements))]
    public void All_message_parsers_are_registered(Requirement requirement)
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
                    [$"{ServiceBusNamespaceOptions.SectionName}__{nameof(ServiceBusNamespaceOptions.FullyQualifiedNamespace)}"] = "Fake",

                    [$"{UserAuthenticationOptions.SectionName}:{nameof(UserAuthenticationOptions.MitIdExternalMetadataAddress)}"] = "NotEmpty",
                    [$"{UserAuthenticationOptions.SectionName}:{nameof(UserAuthenticationOptions.ExternalMetadataAddress)}"] = "NotEmpty",
                    [$"{UserAuthenticationOptions.SectionName}:{nameof(UserAuthenticationOptions.BackendBffAppId)}"] = "NotEmpty",
                    [$"{UserAuthenticationOptions.SectionName}:{nameof(UserAuthenticationOptions.InternalMetadataAddress)}"] = "NotEmpty",
                })
            .Build();

        using var application = new WebApplicationFactory<global::Energinet.DataHub.EDI.B2CWebApi.Program>()
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

    /// <summary>
    /// The <see cref="UnitOfWork"/> uses dependency injection to get a list of generic DbContexts. Those
    /// must be the same references as the specific DbContexts that are registered in the DI container and used in
    /// repositories etc., which this test verifies.
    /// </summary>
    [Fact]
    public void Generic_and_specific_db_contexts_have_same_references()
    {
        using var scope = _host.Services.CreateScope();

        DbContext[] specificContexts =
        [
            scope.ServiceProvider.GetRequiredService<ActorMessageQueueContext>(),
            scope.ServiceProvider.GetRequiredService<IncomingMessagesContext>(),
            scope.ServiceProvider.GetRequiredService<MasterDataContext>(),
            scope.ServiceProvider.GetRequiredService<OutboxContext>(),
        ];

        var genericDbContexts = scope.ServiceProvider.GetServices<IEdiDbContext>()
            .Cast<DbContext>()
            .ToList();

        using var assertionScope = new AssertionScope();
        genericDbContexts.Should().HaveSameCount(specificContexts);

        foreach (var genericDbContext in genericDbContexts)
        {
            var specificDbContext = specificContexts.SingleOrDefault(specific => specific.GetType() == genericDbContext.GetType());
            specificDbContext.Should().NotBeNull();
            var dbContextIsSameReference = ReferenceEquals(specificDbContext, genericDbContext);
            dbContextIsSameReference.Should().BeTrue($"expected {specificDbContext?.GetType().Name} to be the same reference as {genericDbContext.GetType().Name}");
        }
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
            .Select(type => new object[] { new Requirement(type.Name, [targetType], type) });
    }

    private sealed class TestEnvironment : RuntimeEnvironment
    {
        public override string? ServiceBus__SendConnectionString =>
            CreateFakeServiceBusFullyQualifiedNamespace();

        public override string? REQUEST_RESPONSE_LOGGING_CONNECTION_STRING =>
            CreateFakeServiceBusFullyQualifiedNamespace();

        public override string? DB_CONNECTION_STRING =>
            CreateConnectionString();

        public override Uri? AZURE_STORAGE_ACCOUNT_URL => new(CreateFakeStorageUrl());

        public override string AZURE_FUNCTIONS_ENVIRONMENT => "Development";

        public static string CreateFakeServiceBusFullyQualifiedNamespace()
        {
            return new StringBuilder()
                .Append(CultureInfo.InvariantCulture, $"sb://sb-{Guid.NewGuid():N}.servicebus.windows.net/")
                .ToString();
        }

        public static string CreateConnectionString()
        {
            return
                "Server=(LocalDB)\\\\MSSQLLocalDB;Database=B2BTransactions;User=User;Password=Password;TrustServerCertificate=true;Encrypt=True;Trusted_Connection=True;";
        }

        public static string CreateFakeStorageUrl()
        {
            return "https://dummy.url";
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
