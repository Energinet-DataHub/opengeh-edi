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

using System.Text;
using System.Text.Json;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Application.UseCases;
using Energinet.DataHub.EDI.IncomingMessages.Domain.MessageParsers;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Messages;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Validation.ValidationErrors;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.MasterData.Interfaces.Models;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.IntegrationTests.Infrastructure.CimMessageAdapter.Messages.WholesaleServices;

public class IncomingWholesaleServiceTests : TestBase, IAsyncLifetime
{
    private static readonly string PathToJsonSchemaCodeLists = $"Schemas{Path.DirectorySeparatorChar}"
                                                               + $"Cim{Path.DirectorySeparatorChar}"
                                                               + $"Json{Path.DirectorySeparatorChar}"
                                                               + $"Schemas{Path.DirectorySeparatorChar}"
                                                               + $"urn-entsoe-eu-wgedi-codelists.schema.json";

    private readonly IDictionary<(IncomingDocumentType, DocumentFormat), IMessageParser> _messageParsers;
    private readonly ValidateIncomingMessage _validateIncomingMessage;

    public IncomingWholesaleServiceTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
        var authenticatedActor = GetService<AuthenticatedActor>();
        authenticatedActor.SetAuthenticatedActor(new ActorIdentity(ActorNumber.Create("5799999933318"), restriction: Restriction.None,  ActorRole.FromCode("DDQ"), null, ActorId));

        var messageParsers = GetService<IEnumerable<IMessageParser>>();
        _messageParsers = messageParsers
            .ToDictionary(
                parser => (parser.DocumentType, parser.DocumentFormat),
                parser => parser);
        _validateIncomingMessage = GetService<ValidateIncomingMessage>();
    }

    public static IEnumerable<object[]> AllowedActorRolesForWholesaleServices =>
        new List<object[]>
        {
            new object[] { ActorRole.EnergySupplier.Code },
            new object[] { ActorRole.GridAccessProvider.Code },
            new object[] { ActorRole.SystemOperator.Code },
        };

    public static IEnumerable<object[]> JsonMessageTypes()
    {
        var jsonDoc = File.ReadAllText(PathToJsonSchemaCodeLists);

        var stream = new MemoryStream();
        using var writer = new StreamWriter(stream: stream, encoding: Encoding.UTF8, bufferSize: 4096, leaveOpen: true);
        writer.Write(jsonDoc);
        writer.Flush();
        stream.Position = 0;

        var document = JsonDocument.Parse(stream);
        var messageTypesDeclaration = document.RootElement
            .GetProperty("definitions")
            .GetProperty("StandardMessageTypeList");
        var schemaValidMessageTypes = messageTypesDeclaration.GetProperty("enum").EnumerateArray();

        var returnValue = new List<object[]>(schemaValidMessageTypes.Count());
        foreach (var type in schemaValidMessageTypes)
        {
            var value = type.GetString()!;
            returnValue.Add(new object[] { value });
        }

        return returnValue;
    }

    public async Task InitializeAsync()
    {
        await CreateActorIfNotExistAsync(
            new CreateActorDto(
                Energinet.DataHub.EDI.IntegrationTests.Infrastructure.CimMessageAdapter.Messages.TestData.SampleData.StsAssignedUserId,
                ActorNumber.Create(SampleData.SenderId)));

        await CreateActorIfNotExistAsync(
            new CreateActorDto(
                Energinet.DataHub.EDI.IntegrationTests.Infrastructure.CimMessageAdapter.Messages.TestData.SampleData.SecondStsAssignedUserId,
                ActorNumber.Create(Energinet.DataHub.EDI.IntegrationTests.Infrastructure.CimMessageAdapter.Messages.TestData.SampleData.SecondSenderId)));
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Theory]
    [MemberData(nameof(AllowedActorRolesForWholesaleServices))]
    public async Task Given_AllowedActorRoles_When_Validation_Then_ReturnNoErrors(string role)
    {
        // Arrange
        var authenticatedActor = GetService<AuthenticatedActor>();
        authenticatedActor.SetAuthenticatedActor(new ActorIdentity(ActorNumber.Create("5799999933318"), restriction: Restriction.None,  ActorRole.FromCode(role), null, ActorId));

        await using var message = BusinessMessageBuilder
            .RequestWholesaleServices()
            .WithSenderRole(role)
            .Message();

        var (incomingMessage, _) = await ParseWholesaleServicesMessageAsync(message);

        // Act
        var result = await _validateIncomingMessage.ValidateAsync(
            incomingMessage!,
            DocumentFormat.Xml,
            CancellationToken.None);

        // Assert
        //result.Errors.Should().NotContain(e => e is SenderRoleTypeIsNotAuthorized);
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(JsonMessageTypes))]
    public async Task Given_RequestWithMessageTypeNotD21_When_Validating_Then_ReturnError(string type)
    {
        // Arrange
        await using var message = BusinessMessageBuilder
            .RequestWholesaleServices()
            .WithMessageType(type)
            .Message();

        var (incomingMessage, _) = await ParseWholesaleServicesMessageAsync(message);

        // Act
        var result = await _validateIncomingMessage.ValidateAsync(
            incomingMessage!,
            DocumentFormat.Xml,
            CancellationToken.None);

        // Assert
        if (type != "D21")
        {
            result.Errors.Should().ContainSingle()
                .Which.Should().BeOfType<NotSupportedMessageType>();
        }
    }

    [Fact]
    public async Task Given_RequestWithMessageTypeD11_When_Validating_Then_ReturnNoErrors()
    {
        // Arrange
        await using var message = BusinessMessageBuilder
            .RequestWholesaleServices()
            .WithMessageType("D21")
            .Message();

        var (incomingMessage, _) = await ParseWholesaleServicesMessageAsync(message);

        // Act
        var result = await _validateIncomingMessage.ValidateAsync(
            incomingMessage!,
            DocumentFormat.Xml,
            CancellationToken.None);

        // Assert
        result.Errors.Should().BeEmpty();
    }

    private async Task<(RequestWholesaleServicesMessage? IncomingMessage, IncomingMarketMessageParserResult ParserResult)> ParseWholesaleServicesMessageAsync(Stream message)
    {
        if (_messageParsers.TryGetValue((IncomingDocumentType.RequestWholesaleSettlement, DocumentFormat.Xml), out var messageParser))
        {
            var messageParserResult = await messageParser.ParseAsync(new IncomingMarketMessageStream(message), CancellationToken.None).ConfigureAwait(false);

            return (IncomingMessage: (RequestWholesaleServicesMessage?)messageParserResult.IncomingMessage, ParserResult: messageParserResult);
        }

        throw new NotSupportedException($"No message parser found for message format '{DocumentFormat.Xml}' and document type '{IncomingDocumentType.RequestWholesaleSettlement}'");
    }
}
