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

using System.Diagnostics.CodeAnalysis;
using Energinet.DataHub.EDI.AcceptanceTests.Drivers;
using Energinet.DataHub.EDI.AcceptanceTests.Responses.json;
using Energinet.DataHub.EDI.AcceptanceTests.TestData;
using FluentAssertions;

namespace Energinet.DataHub.EDI.AcceptanceTests.Dsl;

public class ArchivedMessageDsl
{
    private readonly EdiB2CDriver _ediB2CDriver;

#pragma warning disable VSTHRD200 // Since this is a DSL we don't want to suffix tasks with 'Async' since it is not part of the ubiquitous language
    public ArchivedMessageDsl(EdiB2CDriver ediB2CDriver)
    {
        _ediB2CDriver = ediB2CDriver;
    }

    internal async Task ConfirmMessageIsArchived(string messageId)
    {
        var archivedMessages = await _ediB2CDriver.RequestArchivedMessageSearchAsync(
            ArchivedMessageData.GetSearchableDataObject(
            messageId!,
            null!,
            null!,
            null!,
            null!)).ConfigureAwait(false);

        archivedMessages.Should().NotBeNull();
        var archivedMessage = archivedMessages.Single();
        Assert.NotNull(archivedMessage.Id);
        Assert.NotNull(archivedMessage.MessageId);
        Assert.NotNull(archivedMessage.DocumentType);
        Assert.NotNull(archivedMessage.SenderNumber);
        Assert.NotNull(archivedMessage.ReceiverNumber);
        Assert.IsType<DateTime>(archivedMessage.CreatedAt);
        Assert.NotNull(archivedMessage.BusinessReason);
    }
}
