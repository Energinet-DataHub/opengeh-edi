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

using Energinet.DataHub.EDI.SubsystemTests.Drivers;
using Energinet.DataHub.EDI.SubsystemTests.Drivers.Ebix;
using Energinet.DataHub.EDI.SubsystemTests.Dsl;
using Xunit.Abstractions;
using Xunit.Categories;

namespace Energinet.DataHub.EDI.SubsystemTests.Tests;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2007", Justification = "Test methods should not call ConfigureAwait(), as it may bypass parallelization limits")]

[IntegrationTest]
[Collection(SubsystemTestCollection.SubsystemTestCollectionName)]
#pragma warning disable xUnit1000 // Skipping the tests in this class, since it's internal
internal sealed class WhenEbixPeekRequestIsReceivedProcessManagerTests : BaseTestClass
#pragma warning restore xUnit1000
{
    private readonly string _meteredDataResponsibleCertificateThumbprint;
    private readonly EbixRequestDsl _ebixMeteredDataResponsible;
    private readonly EbixRequestDsl _ebixEnergySupplier;
    private readonly ActorDsl _actors;
    private readonly CalculationCompletedDsl _calculationCompleted;

    public WhenEbixPeekRequestIsReceivedProcessManagerTests(SubsystemTestFixture fixture, ITestOutputHelper output)
        : base(output, fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        _meteredDataResponsibleCertificateThumbprint = fixture.EbixMeteredDataResponsibleCertificateThumbprint;

        _ebixMeteredDataResponsible = new EbixRequestDsl(
            new EbixDriver(
                fixture.EbixUri,
                fixture.EbixMeteredDataResponsibleCredentials,
                output));

        _ebixEnergySupplier = new EbixRequestDsl(
            new EbixDriver(
                fixture.EbixUri,
                fixture.EbixEnergySupplierCredentials,
                output));

        _actors = new ActorDsl(new MarketParticipantDriver(fixture.EventPublisher), new EdiActorDriver(fixture.ConnectionString));

        _calculationCompleted = new CalculationCompletedDsl(
            new EdiDriver(fixture.DurableClient, fixture.B2BClients.MeteredDataResponsible, output),
            new EdiDatabaseDriver(fixture.ConnectionString),
            new WholesaleDriver(fixture.EventPublisher, fixture.EdiInboxClient),
            new ProcessManagerDriver(fixture.EdiTopicClient),
            output,
            fixture.BalanceFixingCalculationId,
            fixture.WholesaleFixingCalculationId);
    }

    [Fact]
    public async Task Actor_can_peek_and_dequeue_energy_result_in_ebIX_format()
    {
        await _ebixMeteredDataResponsible.EmptyQueueForActor();

        await _calculationCompleted.PublishBrs023_027BalanceFixingCalculation();

        await _ebixMeteredDataResponsible.ConfirmEnergyResultIsAvailable();
    }

    // TODO: Find ebIX actor with Wholesale data, or get Wholesale to create data for ebIX energy supplier actor 5790000610976 in grid area 543
    [Fact(Skip = "The ebIX energy supplier actor 5790000610976 (in grid area 543) does not currently have any Wholesale data")]
    public async Task Actor_can_peek_and_dequeue_wholesale_result_in_ebIX_format()
    {
        await _ebixEnergySupplier.EmptyQueueForActor();

        await _calculationCompleted.PublishBrs023_027WholesaleFixingCalculation();

        await _ebixEnergySupplier.ConfirmWholesaleResultIsAvailable();
    }

    [Fact]
    public async Task Dequeue_request_without_content_gives_ebIX_error_B2B_900()
    {
        await _ebixMeteredDataResponsible.ConfirmInvalidDequeueRequestGivesEbixError();
    }

    [Fact]
    public async Task Dequeue_request_with_incorrect_message_id_gives_ebIX_error_B2B_201()
    {
        await _ebixMeteredDataResponsible.ConfirmDequeueWithIncorrectMessageIdGivesEbixError();
    }

    [Fact]
    public async Task Actor_cannot_peek_ebix_api_without_certificate()
    {
        await _ebixMeteredDataResponsible.ConfirmPeekWithoutCertificateIsNotAllowed();
    }

    [Fact]
    public async Task Actor_cannot_dequeue_ebix_api_without_certificate()
    {
        await _ebixMeteredDataResponsible.ConfirmDequeueWithoutCertificateIsNotAllowed();
    }

    [Fact]
    public async Task Actor_cannot_peek_when_certificate_has_been_removed()
    {
        await _actors.PublishActorCertificateCredentialsRemoved(SubsystemTestFixture.ActorNumber, SubsystemTestFixture.ActorRole, _meteredDataResponsibleCertificateThumbprint);

        await _ebixMeteredDataResponsible.ConfirmPeekWithRemovedCertificateIsNotAllowed();

        await _actors.PublishActorCertificateCredentialsAssigned(SubsystemTestFixture.ActorNumber, SubsystemTestFixture.ActorRole, _meteredDataResponsibleCertificateThumbprint);
    }

    [Fact]
    public async Task Actor_cannot_dequeue_when_certificated_has_been_removed()
    {
        await _actors.PublishActorCertificateCredentialsRemoved(SubsystemTestFixture.ActorNumber, SubsystemTestFixture.ActorRole, _meteredDataResponsibleCertificateThumbprint);

        await _ebixMeteredDataResponsible.ConfirmDequeueWithRemovedCertificateIsNotAllowed();

        await _actors.PublishActorCertificateCredentialsAssigned(SubsystemTestFixture.ActorNumber, SubsystemTestFixture.ActorRole, _meteredDataResponsibleCertificateThumbprint);
    }
}
