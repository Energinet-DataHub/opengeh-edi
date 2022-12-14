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

using System.Threading.Tasks;
using Messaging.Application.OutgoingMessages;
using Messaging.Domain.OutgoingMessages;
using Messaging.Infrastructure.OutgoingMessages.Common.Xml;
using NodaTime;
using Xunit;

namespace Messaging.Tests.Application.OutgoingMessages;

public class DocumentFactoryTests
{
    [Fact]
    public async Task Throw_if_document_format_can_not_be_handled()
    {
        var factory = new DocumentFactory(System.Array.Empty<DocumentWriter>());
        var header = new MessageHeader(
            "ProcessType",
            "SenderId",
            "SenderRole",
            "ReceiverId",
            "ReceiverRole",
            "MessageID",
            SystemClock.Instance.GetCurrentInstant());
        var message = new CimMessage(
            MessageType.GenericNotification,
            header,
            System.Array.Empty<string>());

        await Assert.ThrowsAsync<OutgoingMessageException>(() => factory.CreateFromAsync(message, CimFormat.Xml)).ConfigureAwait(false);
    }
}
