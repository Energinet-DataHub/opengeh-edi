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
using System.Threading.Tasks;
using Application.SearchMessages;
using Domain.ArchivedMessages;
using Infrastructure.SearchMessages;
using IntegrationTests.Fixtures;
using Xunit;

namespace IntegrationTests.Application.SearchMessages;

public class SearchMessagesTests : TestBase
{
    private readonly ArchivedMessageRepository _repository;

    public SearchMessagesTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _repository = new ArchivedMessageRepository();
    }

    [Fact]
    public async Task Can_fetch_messages()
    {
        var messageId = Guid.NewGuid();
        _repository.Add(new ArchivedMessage(messageId));

        var result = await InvokeQueryAsync(new GetMessagesQuery()).ConfigureAwait(false);

        Assert.NotNull(result);
        Assert.Contains(result.Messages, message => message.MessageId == messageId);
    }

    #pragma warning disable
    private async Task<MessageSearchResult?> InvokeQueryAsync(GetMessagesQuery query)
    {
        var messages = await _repository.GetAllAsync().ConfigureAwait(false);
        return new MessageSearchResult(messages.Select(message => new MessageInfo(message.MessageId)).ToList().AsReadOnly());
    }
}
