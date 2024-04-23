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

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Energinet.DataHub.EDI.AcceptanceTests.Drivers;
using Energinet.DataHub.EDI.AcceptanceTests.Dsl;
using Energinet.DataHub.EDI.AcceptanceTests.Factories;
using Energinet.DataHub.EDI.AcceptanceTests.TestData;
using FluentAssertions;
using Xunit.Abstractions;
#pragma warning disable CS0162 // Unreachable code detected

namespace Energinet.DataHub.EDI.AcceptanceTests.Tests.ArchivedMessages;

[Collection("Acceptance test collection")]
[SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Testing")]
public class WhenArchivedMessageIsRequestedTests : BaseTestClass
{
    private readonly ArchivedMessageDsl _archivedMessageDsl;
    private readonly NotifyWholesaleServicesDsl _notifyWholesaleServicesDsl;

    public WhenArchivedMessageIsRequestedTests(ITestOutputHelper output, AcceptanceTestFixture fixture)
        : base(output, fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        _archivedMessageDsl = new ArchivedMessageDsl(
            new EdiB2CDriver(fixture.B2CAuthorizedHttpClient, fixture.ApiManagementUri));

        _notifyWholesaleServicesDsl = new NotifyWholesaleServicesDsl(
            new EdiDriver(fixture.B2BEnergySupplierAuthorizedHttpClient),
            new WholesaleDriver(fixture.EventPublisher, fixture.EdiInboxClient));
    }

    [Fact]
    public async Task B2C_actor_can_get_the_archived_message_after_peeking_the_message()
    {
        await _notifyWholesaleServicesDsl.EmptyQueueForActor();

        await _notifyWholesaleServicesDsl.PublishMonthlyChargeResultFor(
            AcceptanceTestFixture.CimActorGridArea,
            AcceptanceTestFixture.EdiSubsystemTestCimEnergySupplierNumber,
            AcceptanceTestFixture.ActorNumber);

        var messageId = await _notifyWholesaleServicesDsl.ConfirmResultIsAvailableFor();

        var archivedMessages = await _archivedMessageDsl.GetMessageIsArchived(messageId);

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
