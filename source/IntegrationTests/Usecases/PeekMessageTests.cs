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
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
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

    public PeekMessageTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
        _dateTimeProvider = (SystemDateTimeProviderStub)GetService<ISystemDateTimeProvider>();
        _connectionFactory = GetService<IDatabaseConnectionFactory>();
    }

    [Fact]
    public async Task Peek_returns_the_oldest_bundle()
    {
        var sut = GetService<PeekMessage>();
        var outgoingMessagesClient = GetService<IOutgoingMessagesClient>();
        var receiver = Receiver.Create(ActorNumber.Create("1234567890123"), ActorRole.EnergySupplier);
        var dtoBuilder = new WholesaleTotalAmountMessageDtoBuilder();

        var message1 = dtoBuilder.WithReceiverNumber(receiver.Number).Build();
        var message2 = dtoBuilder.WithReceiverNumber(receiver.Number).Build();

        var now = Instant.FromUtc(2024, 05, 07, 13, 37);
        _dateTimeProvider.SetNow(now);
        await outgoingMessagesClient.EnqueueAndCommitAsync(message1, CancellationToken.None);
        _dateTimeProvider.SetNow(now.Plus(Duration.FromDays(1)));
        await outgoingMessagesClient.EnqueueAndCommitAsync(message2, CancellationToken.None);

        var peekResultDto = await sut.PeekAsync(
                new PeekRequestDto(
                    receiver.Number,
                    MessageCategory.Aggregations,
                    receiver.ActorRole,
                    DocumentFormat.Json),
                CancellationToken.None);
        using var connection = await _connectionFactory.GetConnectionAndOpenAsync(CancellationToken.None);
        var sql = $"SELECT MessageId FROM Bundles WHERE Created = (SELECT MAX(Created) FROM Bundles)";
        var outgoingMessageIdFromOldestBundle = await connection.QuerySingleOrDefaultAsync<string>(sql);
        peekResultDto.Should().NotBeNull();
        peekResultDto!.MessageId.Value.Should().Be(outgoingMessageIdFromOldestBundle);
    }
}
