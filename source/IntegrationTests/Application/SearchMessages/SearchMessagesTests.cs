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
using Domain.OutgoingMessages;
using IntegrationTests.Fixtures;
using Xunit;

namespace IntegrationTests.Application.SearchMessages;

public class SearchMessagesTests : TestBase
{
    private readonly IArchivedMessageRepository _repository;

    public SearchMessagesTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _repository = GetService<IArchivedMessageRepository>();
    }

    [Fact]
    public async Task Can_fetch_messages()
    {
        var archivedMessage = new ArchivedMessage(Guid.NewGuid(), MessageType.AccountingPointCharacteristics);
        _repository.Add(archivedMessage);

        var result = await QueryAsync(new GetMessagesQuery()).ConfigureAwait(false);

        var messageInfo = result.Messages.FirstOrDefault(message => message.MessageId == archivedMessage.MessageId);

        Assert.NotNull(messageInfo);
        Assert.Equal(archivedMessage.DocumentType.Name, messageInfo.DocumentType);
    }
}
