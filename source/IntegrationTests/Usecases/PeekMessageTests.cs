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

using Dapper;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.IntegrationTests.TestDoubles;
using Energinet.DataHub.EDI.OutgoingMessages.Application.UseCases;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.ActorMessagesQueues;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.Dequeue;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.Peek;
using Energinet.DataHub.EDI.Tests.Factories;
using FluentAssertions;
using NodaTime;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.IntegrationTests.Usecases;

public class PeekMessageTests
    : TestBase
{
    private readonly SystemDateTimeProviderStub _dateTimeProvider;
    private readonly IDatabaseConnectionFactory _connectionFactory;
    private readonly Receiver _receiver;

    public PeekMessageTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
        _dateTimeProvider = (SystemDateTimeProviderStub)GetService<ISystemDateTimeProvider>();
        _connectionFactory = GetService<IDatabaseConnectionFactory>();
        _receiver = Receiver.Create(ActorNumber.Create("1234567890123"), ActorRole.EnergySupplier);
    }

    [Fact]
    public async Task Peek_returns_the_oldest_bundle()
    {
        var sut = GetService<PeekMessage>();
        var outgoingMessagesClient = GetService<IOutgoingMessagesClient>();
        var dtoBuilder = new WholesaleTotalAmountMessageDtoBuilder();

        var message1 = dtoBuilder.WithReceiverNumber(_receiver.Number).Build();
        var message2 = dtoBuilder.WithReceiverNumber(_receiver.Number).Build();

        var now = Instant.FromUtc(2024, 05, 07, 13, 37);
        _dateTimeProvider.SetNow(now);
        await outgoingMessagesClient.EnqueueAndCommitAsync(message1, CancellationToken.None);
        _dateTimeProvider.SetNow(now.Plus(Duration.FromDays(1)));
        await outgoingMessagesClient.EnqueueAndCommitAsync(message2, CancellationToken.None);

        var peekResultDto = await sut.PeekAsync(
                new PeekRequestDto(
                    _receiver.Number,
                    MessageCategory.Aggregations,
                    _receiver.ActorRole,
                    DocumentFormat.Json),
                CancellationToken.None);
        using var connection = await _connectionFactory.GetConnectionAndOpenAsync(CancellationToken.None);
        const string sql = $"SELECT MessageId FROM Bundles WHERE Created = (SELECT MAX(Created) FROM Bundles)";
        var messageIdFromOldestBundle = await connection.QuerySingleOrDefaultAsync<string>(sql);

        peekResultDto.Should().NotBeNull();
        messageIdFromOldestBundle.Should().Be(peekResultDto!.MessageId.Value);
    }

    [Fact]
    public async Task When_no_message_has_been_enqueued_peek_returns_no_bundle_id()
     {
         var sut = GetService<PeekMessage>();

         var result = await sut.PeekAsync(
             new PeekRequestDto(
                 _receiver.Number,
                 MessageCategory.Aggregations,
                 _receiver.ActorRole,
                 DocumentFormat.Json),
             CancellationToken.None);

         result.Should().BeNull();
     }

    [Fact]
    public async Task Peek_returns_null_if_bundle_has_been_dequeued()
     {
         var sut = GetService<PeekMessage>();
         var outgoingMessagesClient = GetService<IOutgoingMessagesClient>();
         var dtoBuilder = new WholesaleTotalAmountMessageDtoBuilder();

         var message = dtoBuilder.WithReceiverNumber(_receiver.Number).Build();

         await outgoingMessagesClient.EnqueueAndCommitAsync(message, CancellationToken.None);

         var peekResult = await sut.PeekAsync(
             new PeekRequestDto(
                 _receiver.Number,
                 MessageCategory.Aggregations,
                 _receiver.ActorRole,
                 DocumentFormat.Json),
             CancellationToken.None);

         peekResult.Should().NotBeNull();

         var dequeueResult = await outgoingMessagesClient.DequeueAndCommitAsync(new DequeueRequestDto(peekResult!.MessageId.Value, _receiver.ActorRole, _receiver.Number), CancellationToken.None);
         dequeueResult.Success.Should().BeTrue();

         var secondPeekResult = await sut.PeekAsync(
             new PeekRequestDto(
                 _receiver.Number,
                 MessageCategory.Aggregations,
                 _receiver.ActorRole,
                 DocumentFormat.Json),
             CancellationToken.None);

         secondPeekResult.Should().BeNull();
     }
}
