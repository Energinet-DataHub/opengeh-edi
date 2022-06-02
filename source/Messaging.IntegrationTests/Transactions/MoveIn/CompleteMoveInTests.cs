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
using System.Threading.Tasks;
using Messaging.Application.Transactions.MoveIn;
using Messaging.IntegrationTests.Fixtures;
using Xunit;

namespace Messaging.IntegrationTests.Transactions.MoveIn;

public class CompleteMoveInTests : TestBase
{
    public CompleteMoveInTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
    }

    [Fact]
    public async Task Transaction_must_exist()
    {
        var processId = "Not existing";
        var command = new CompleteMoveInTransaction(processId);
        var handler = new CompleteMoveInTransactionHandler(GetService<IMoveInTransactionRepository>());

        await Assert.ThrowsAsync<TransactionNotFoundException>(() => handler.HandleAsync(command)).ConfigureAwait(false);
    }
}

#pragma warning disable
public class TransactionNotFoundException : Exception
{
    public TransactionNotFoundException(string processId)
    : base($"Could not find a transaction for business process id {processId}")
    {
    }
}

public class CompleteMoveInTransactionHandler
{
    private readonly IMoveInTransactionRepository _transactionRepository;

    public CompleteMoveInTransactionHandler(IMoveInTransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task HandleAsync(CompleteMoveInTransaction command)
    {
        var transaction = await _transactionRepository.GetByProcessIdAsync(command.ProcessId).ConfigureAwait(false);
        if (transaction is null)
        {
            throw new TransactionNotFoundException(command.ProcessId);
        }
    }
}

public class CompleteMoveInTransaction
{
    public CompleteMoveInTransaction(string processId)
    {
        ProcessId = processId;
    }

    public string ProcessId { get; }
}
