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
using Energinet.DataHub.EDI.AcceptanceTests.Dsl;
using FluentAssertions;
using Xunit.Abstractions;
#pragma warning disable CS0162 // Unreachable code detected

namespace Energinet.DataHub.EDI.AcceptanceTests.Tests.ArchivedMessages;

[Collection("Acceptance test collection")]
[SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Testing")]
public class WhenArchivedMessageIsRequestedTests : BaseTestClass
{
    private readonly ArchivedMessageDsl _archivedMessage;
    private readonly NotifyWholesaleServicesDsl _notifyWholesaleServices;
    private readonly CalculationCompletedDsl _calculationCompleted;

    public WhenArchivedMessageIsRequestedTests(ITestOutputHelper output, AcceptanceTestFixture fixture)
        : base(output, fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        _archivedMessage = new ArchivedMessageDsl(
            new EdiB2CDriver(fixture.B2CAuthorizedHttpClient, fixture.ApiManagementUri));

        _calculationCompleted = new CalculationCompletedDsl(
            fixture,
            new EdiDriver(fixture.DurableClient, fixture.B2BEnergySupplierAuthorizedHttpClient, output),
            new WholesaleDriver(fixture.EventPublisher, fixture.EdiInboxClient));

        _notifyWholesaleServices = new NotifyWholesaleServicesDsl(
            fixture,
            new EdiDriver(fixture.DurableClient, fixture.B2BEnergySupplierAuthorizedHttpClient, output),
            new WholesaleDriver(fixture.EventPublisher, fixture.EdiInboxClient));
    }

    [Fact]
    public async Task B2C_actor_can_get_the_archived_message_after_peeking_the_message()
    {
        await _calculationCompleted.PublishForWholesaleFixingCalculation();

        var messageId = await _notifyWholesaleServices.ConfirmResultIsAvailable();

        await _archivedMessage.ConfirmMessageIsArchived(messageId);
    }
}
