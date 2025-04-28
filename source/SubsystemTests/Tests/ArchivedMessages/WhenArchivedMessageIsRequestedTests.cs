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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.SubsystemTests.Drivers;
using Energinet.DataHub.EDI.SubsystemTests.Drivers.B2C;
using Energinet.DataHub.EDI.SubsystemTests.Drivers.Ebix;
using Energinet.DataHub.EDI.SubsystemTests.Dsl;
using Energinet.DataHub.EDI.SubsystemTests.TestOrdering;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.SubsystemTests.Tests.ArchivedMessages;

[Collection(SubsystemTestCollection.SubsystemTestCollectionName)]
[SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Testing")]
public sealed class WhenArchivedMessageIsRequestedTests : BaseTestClass
{
    private readonly ArchivedMessageDsl _archivedMessages;
    private readonly NotifyAggregatedMeasureDataResultDsl _notifyAggregatedMeasureData;
    private readonly CalculationCompletedDsl _calculationCompleted;
    private readonly ForwardMeteredDataDsl _forwardMeteredDataAsGridAccessProvider;

    public WhenArchivedMessageIsRequestedTests(ITestOutputHelper output, SubsystemTestFixture fixture)
        : base(output, fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        var ediDatabaseDriver = new EdiDatabaseDriver(fixture.ConnectionString);

        _archivedMessages = new ArchivedMessageDsl(
            new B2CEdiDriver(fixture.B2CClients.DatahubAdministrator, fixture.ApiManagementUri, fixture.EdiB2CWebApiUri, output),
            ediDatabaseDriver);

        var ediDriver = new EdiDriver(fixture.DurableClient, fixture.B2BClients.MeteredDataResponsible, output);
        var processManagerDriver = new ProcessManagerDriver(fixture.EdiTopicClient);
        _calculationCompleted = new CalculationCompletedDsl(
            ediDriver,
            ediDatabaseDriver,
            processManagerDriver,
            output,
            fixture.BalanceFixingCalculationId,
            fixture.WholesaleFixingCalculationId);
        _forwardMeteredDataAsGridAccessProvider = new ForwardMeteredDataDsl(
            ebix: new EbixDriver(
                fixture.EbixUri,
                fixture.EbixGridAccessProviderCredentials,
                output),
            ediDriver: new EdiDriver(
                fixture.DurableClient,
                fixture.B2BClients.GridAccessProvider,
                output),
            ediDatabaseDriver: ediDatabaseDriver,
            processManagerDriver: processManagerDriver);
        _notifyAggregatedMeasureData = new NotifyAggregatedMeasureDataResultDsl(ediDriver);
    }

    [Fact]
    public async Task B2C_actor_can_get_the_archived_message_after_peeking_the_message()
    {
        await _calculationCompleted.PublishBrs023_027BalanceFixingCalculation();

        var messageId = await _notifyAggregatedMeasureData.ConfirmResultIsAvailable();

        await _archivedMessages.ConfirmMessageIsArchivedV3(messageId);
    }

    [Fact]
    public async Task Audit_log_outbox_is_published_after_searching_for_archived_message_with_pagination()
    {
        var (messageId, createdAfter) = await _archivedMessages.PerformArchivedMessageSearchV2(pageSize: 10);
        await _archivedMessages.ConfirmArchivedMessageSearchAuditLogExistsForMessageId(messageId, createdAfter);
    }

    [Fact]
    public async Task Audit_log_outbox_is_published_after_searching_for_archived_message_with_pagination_v3()
    {
        var (messageId, createdAfter) = await _archivedMessages.PerformArchivedMessageSearchV3(pageSize: 10);
        await _archivedMessages.ConfirmArchivedMessageSearchAuditLogExistsForMessageId(messageId, createdAfter);
    }

    [Fact]
    public async Task B2C_actor_can_get_the_send_forward_metering_point_archived_message()
    {
        var meteringPointId = MeteringPointId.From("9999999999");
        await _forwardMeteredDataAsGridAccessProvider
            .SendForwardMeteredDataInCimAsync(meteringPointId, CancellationToken.None);

        await _archivedMessages.ConfirmMeteringPointArchivedMessageSearch(meteringPointId);
    }
}
