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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Dapper;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.IntegrationTests.Assertions;
using Energinet.DataHub.EDI.IntegrationTests.Factories;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.IntegrationTests.TestDoubles;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using FluentAssertions;
using FluentAssertions.Execution;
using NodaTime;
using Xunit;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.OutgoingMessages;

public class WhenAPeekIsRequestedTests : TestBase
{
    private readonly EnergyResultMessageDtoBuilder _energyResultMessageDtoBuilder;
    private readonly IOutgoingMessagesClient _outgoingMessagesClient;
    private readonly SystemDateTimeProviderStub _dateTimeProvider;

    public WhenAPeekIsRequestedTests(IntegrationTestFixture integrationTestFixture)
        : base(integrationTestFixture)
    {
        _energyResultMessageDtoBuilder = new EnergyResultMessageDtoBuilder();
        _outgoingMessagesClient = GetService<IOutgoingMessagesClient>();
        _dateTimeProvider = (SystemDateTimeProviderStub)GetService<ISystemDateTimeProvider>();
    }

    public static object[][] GetUnusedDataHubTypesWithDocumentFormat()
    {
        var documentFormats = EnumerationType.GetAll<DocumentFormat>();
        var dataHubTypesWithUnused = Assembly.GetAssembly(typeof(DataHubTypeWithUnused<>))!
            .GetTypes()
            .Where(
                t => t.BaseType is { IsGenericType: true }
                     && t.BaseType.GetGenericTypeDefinition() == typeof(DataHubTypeWithUnused<>))
            .ToArray();

        var typesWithDocumentFormats = dataHubTypesWithUnused
            .SelectMany(t => documentFormats.Select(df => new object[] { t, df }))
            .ToArray();

        return typesWithDocumentFormats;
    }

    [Fact]
    public async Task When_no_messages_are_available_return_empty_result()
    {
        var message = _energyResultMessageDtoBuilder.Build();
        await EnqueueMessage(message);

        var result = await PeekMessageAsync(MessageCategory.None);

        Assert.Null(result.Bundle);
        Assert.True(await BundleIsRegistered());
    }

    [Fact]
    public async Task A_message_bundle_is_returned()
    {
        var message = _energyResultMessageDtoBuilder
            .WithReceiverNumber(SampleData.NewEnergySupplierNumber)
            .WithReceiverRole(ActorRole.EnergySupplier)
            .Build();
        await EnqueueMessage(message);

        var result = await PeekMessageAsync(MessageCategory.Aggregations);

        AssertXmlMessage.Document(XDocument.Load(result.Bundle!))
            .IsDocumentType(DocumentType.NotifyAggregatedMeasureData)
            .IsBusinessReason(BusinessReason.BalanceFixing)
            .HasSerieRecordCount(1);
    }

    [Fact]
    public async Task Ensure_same_bundle_is_returned_if_not_dequeued()
    {
        var message = _energyResultMessageDtoBuilder
            .WithReceiverNumber(SampleData.NewEnergySupplierNumber)
            .WithReceiverRole(ActorRole.EnergySupplier)
            .Build();
        await EnqueueMessage(message);

        var firstPeekResult = await PeekMessageAsync(MessageCategory.Aggregations);

        ClearDbContextCaches(); // Else the MarketDocument is cached in Entity Framework
        var secondPeekResult = await PeekMessageAsync(MessageCategory.Aggregations);

        Assert.NotNull(firstPeekResult.MessageId);
        Assert.NotNull(secondPeekResult.MessageId);

        var firstPeekContent = await GetStreamContentAsStringAsync(firstPeekResult.Bundle!);
        var secondPeekContent = await GetStreamContentAsStringAsync(secondPeekResult.Bundle!);
        Assert.Equal(firstPeekContent, secondPeekContent);
    }

    [Fact]
    public async Task A_market_document_is_archived_with_correct_content()
    {
        // Arrange
        var message = _energyResultMessageDtoBuilder
            .WithReceiverNumber(SampleData.NewEnergySupplierNumber)
            .WithReceiverRole(ActorRole.EnergySupplier)
            .Build();
        await EnqueueMessage(message);

        // Act
        var peekResult = await PeekMessageAsync(MessageCategory.Aggregations);

        // Assert
        using var assertScope = new AssertionScope();
        peekResult.Bundle.Should().NotBeNull();

        var peekResultFileContent = await GetStreamContentAsStringAsync(peekResult.Bundle!);
        var archivedMessageFileStorageReference = await GetArchivedMessageFileStorageReferenceFromDatabaseAsync(peekResult.MessageId!.Value);
        archivedMessageFileStorageReference.Should().NotBeNull();

        var archivedMessageFileContent = await GetFileContentFromFileStorageAsync(
            "archived",
            archivedMessageFileStorageReference!);

        archivedMessageFileContent.Should().Be(peekResultFileContent);
    }

    [Fact]
    public async Task A_market_document_is_archived_with_correct_file_storage_reference()
    {
        // Arrange
        int year = 2024,
            month = 01,
            date = 02;
        _dateTimeProvider.SetNow(Instant.FromUtc(year, month, date, 11, 07));
        var receiverNumber = SampleData.NewEnergySupplierNumber;
        var message = _energyResultMessageDtoBuilder
            .WithReceiverNumber(receiverNumber)
            .WithReceiverRole(ActorRole.EnergySupplier)
            .Build();
        await EnqueueMessage(message);

        // Act
        var result = await PeekMessageAsync(MessageCategory.Aggregations);

        // Assert
        result.Bundle.Should().NotBeNull();

        var archivedMessageId = await GetArchivedMessageIdFromDatabaseAsync(result.MessageId!.Value.ToString());
        var fileStorageReference = await GetArchivedMessageFileStorageReferenceFromDatabaseAsync(result.MessageId!.Value);
        fileStorageReference.Should().Be($"{receiverNumber}/{year:0000}/{month:00}/{date:00}/{archivedMessageId:N}");
    }

    [Fact]
    public async Task A_market_document_is_added_to_database()
    {
        var message = _energyResultMessageDtoBuilder
            .WithReceiverNumber(SampleData.NewEnergySupplierNumber)
            .WithReceiverRole(ActorRole.EnergySupplier)
            .Build();
        await EnqueueMessage(message);

        var result = await PeekMessageAsync(MessageCategory.Aggregations);

        result.MessageId.Should().NotBeNull();

        var marketDocumentExists = await MarketDocumentExists(result.MessageId!.Value);
        marketDocumentExists.Should().BeTrue();
    }

    [Fact]
    public async Task The_created_market_document_uses_the_archived_message_file_reference()
    {
        var message = _energyResultMessageDtoBuilder
            .WithReceiverNumber(SampleData.NewEnergySupplierNumber)
            .WithReceiverRole(ActorRole.EnergySupplier)
            .Build();
        await EnqueueMessage(message);

        var peekResult = await PeekMessageAsync(MessageCategory.Aggregations);

        var marketDocumentFileStorageReference = await GetMarketDocumentFileStorageReferenceFromDatabaseAsync(peekResult.MessageId!.Value);
        var archivedMessageFileStorageReference = await GetArchivedMessageFileStorageReferenceFromDatabaseAsync(peekResult.MessageId!.Value);

        marketDocumentFileStorageReference.Should().Be(archivedMessageFileStorageReference);
    }

    /// <summary>
    /// This test verifies the "hack" for a MDR/GridOperator actor which is the same Actor but with two distinct roles MDR and GridOperator
    /// The actor uses the MDR (MeteredDataResponsible) role when making request (RequestAggregatedMeasureData)
    /// but uses the DDM (GridOperator) role when peeking.
    /// This means that when peeking as a MDR we should peek the DDM queue
    /// </summary>
    [Fact]
    public async Task When_PeekingAsMeteredDataResponsible_Then_FindsGridOperatorMessages()
    {
        // Arrange
        var actorNumber = ActorNumber.Create("1234567890123");
        var message = _energyResultMessageDtoBuilder
            .WithReceiverNumber(actorNumber.Value)
            .WithReceiverRole(ActorRole.GridOperator)
            .Build();
        await EnqueueMessage(message);

        // Act
        var peekResult = await PeekMessageAsync(MessageCategory.Aggregations, actorNumber: actorNumber, actorRole: ActorRole.MeteredDataResponsible);

        // Assert
        peekResult.MessageId.Should().NotBeNull();
    }

    [Theory]
    [MemberData(nameof(GetUnusedDataHubTypesWithDocumentFormat))]
    public async Task When_unused_datahub_value_then_exception_is_thrown(Type dataHubTypeWithUnused, DocumentFormat documentFormat)
    {
        ArgumentNullException.ThrowIfNull(dataHubTypeWithUnused);
        ArgumentNullException.ThrowIfNull(documentFormat);

        string unusedCode;

        var builder = _energyResultMessageDtoBuilder
            .WithReceiverNumber(SampleData.NewEnergySupplierNumber)
            .WithReceiverRole(ActorRole.EnergySupplier);

        if (dataHubTypeWithUnused == typeof(BusinessReason))
        {
            unusedCode = "A47";
            builder.WithBusinessReason(BusinessReason.FromCodeOrUnused(unusedCode));
        }
        else if (dataHubTypeWithUnused == typeof(SettlementVersion))
        {
            unusedCode = "D10";
            builder.WithSettlementVersion(SettlementVersion.FromCodeOrUnused(unusedCode));
        }
        else
        {
            throw new NotImplementedException($"Type {dataHubTypeWithUnused.Name} is not implemented yet");
        }

        var message = _energyResultMessageDtoBuilder.Build();

        var act = async () =>
        {
            await EnqueueMessage(message);
            var result = await PeekMessageAsync(MessageCategory.Aggregations, message.ReceiverNumber, message.ReceiverRole, documentFormat: documentFormat);
            return result;
        };

        (await act.Should().ThrowAsync<InvalidOperationException>($"because {unusedCode} is a unused {dataHubTypeWithUnused.Name} code"))
            .WithMessage($"{unusedCode} is not a valid {dataHubTypeWithUnused.Name}*");
    }

    private async Task<bool> BundleIsRegistered()
    {
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var numberOfBundles = await connection
            .ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Bundles");
        return numberOfBundles == 1;
    }

    private async Task<bool> MarketDocumentExists(Guid marketDocumentBundleId)
    {
        var sqlStatement =
            $"SELECT COUNT(*) FROM [dbo].[MarketDocuments] WHERE BundleId = '{marketDocumentBundleId}'";
        using var connection =
            await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var exists = await connection.ExecuteScalarAsync<bool>(sqlStatement);

        return exists;
    }

    private async Task EnqueueMessage(EnergyResultMessageDto message)
    {
        await _outgoingMessagesClient.EnqueueAndCommitAsync(message, CancellationToken.None);
    }
}
