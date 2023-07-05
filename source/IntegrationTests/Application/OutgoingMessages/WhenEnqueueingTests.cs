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

using System.Threading;
using System.Threading.Tasks;
using Application.Configuration.DataAccess;
using Application.OutgoingMessages;
using Dapper;
using Domain.Actors;
using Domain.Documents;
using Domain.OutgoingMessages;
using Domain.Transactions;
using Infrastructure.OutgoingMessages;
using IntegrationTests.Fixtures;
using Xunit;

namespace IntegrationTests.Application.OutgoingMessages;

public class WhenEnqueueingTests : TestBase
{
    public WhenEnqueueingTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
    }

    [Fact]
    public async Task Outgoing_message_is_enqueued()
    {
        var message = new OutgoingMessage(
            DocumentType.NotifyAggregatedMeasureData,
            ActorNumber.Create("1123456789101"),
            TransactionId.New(),
            BusinessReason.BalanceFixing.Name,
            MarketRole.MeteredDataResponsible,
            ActorNumber.Create("1123456789102"),
            MarketRole.MeteringDataAdministrator,
            "MessageRecord");
        var messageEnqueuer = GetService<MessageEnqueuer>();

        messageEnqueuer.Enqueue(message);
        var unitOfWork = GetService<IUnitOfWork>();
        await unitOfWork.CommitAsync();

        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None).ConfigureAwait(false);
        var sql = $"SELECT * FROM [dbo].[OutgoingMessages]";
        var result = await
            connection
                .QuerySingleOrDefaultAsync(sql)
                .ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.Equal(result.DocumentType, message.DocumentType.Name);
        Assert.Equal(result.ReceiverId, message.ReceiverId.Value);
        Assert.Equal(result.ReceiverRole, message.ReceiverRole.Name);
        Assert.Equal(result.SenderId, message.SenderId.Value);
        Assert.Equal(result.SenderRole, message.SenderRole.Name);
        Assert.Equal(result.BusinessReason, message.BusinessReason);
        Assert.NotNull(result.MessageRecord);
        Assert.NotNull(result.AssignedBundleId);
    }
}
