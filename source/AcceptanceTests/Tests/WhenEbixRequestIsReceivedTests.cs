﻿// Copyright 2020 Energinet DataHub A/S
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

using Energinet.DataHub.EDI.AcceptanceTests.Drivers;
using Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix;
using Energinet.DataHub.EDI.AcceptanceTests.Dsl;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Xunit.Abstractions;
using Xunit.Categories;

namespace Energinet.DataHub.EDI.AcceptanceTests.Tests;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2007", Justification = "Test methods should not call ConfigureAwait(), as it may bypass parallelization limits")]

[IntegrationTest]
[Collection(AcceptanceTestCollection.AcceptanceTestCollectionName)]
public sealed class WhenEbixPeekRequestIsReceivedTests : BaseTestClass
{
    private readonly EbixRequestDsl _ebixMDR;
    private readonly ActorDsl _actor;
    private readonly EbixRequestDsl _ebixEs;
    private readonly CalculationCompletedDsl _calculationCompleted;
    private readonly string _ebixCertificateThumbprint;

    public WhenEbixPeekRequestIsReceivedTests(AcceptanceTestFixture fixture, ITestOutputHelper output)
        : base(output, fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        _ebixCertificateThumbprint = fixture.EbixCertificateThumbprint;

        _ebixMDR = new EbixRequestDsl(
            new EbixDriver(
                new Uri(fixture.EbixUri, "/ebix"),
                fixture.EbixCertificatePasswordForMeterDataResponsible,
                ActorRole.MeteredDataAdministrator));

        _ebixEs = new EbixRequestDsl(
            new EbixDriver(
                new Uri(fixture.EbixUri, "/ebix"),
                fixture.EbixCertificatePasswordForEnergySupplier,
                ActorRole.EnergySupplier));

        _actor = new ActorDsl(new MarketParticipantDriver(fixture.EventPublisher), new EdiActorDriver(fixture.ConnectionString));

        _calculationCompleted = new CalculationCompletedDsl(
            new EdiDriver(fixture.DurableClient, fixture.B2BMeteredDataResponsibleAuthorizedHttpClient, output),
            new EdiDatabaseDriver(fixture.ConnectionString),
            new WholesaleDriver(fixture.EventPublisher, fixture.EdiInboxClient),
            output,
            fixture.BalanceFixingCalculationId,
            fixture.WholesaleFixingCalculationId);
    }

    [Fact]
    public async Task Actor_can_peek_and_dequeue_energy_result_in_ebIX_format()
    {
        await _ebixMDR.EmptyQueueForActor();

        await _calculationCompleted.PublishForBalanceFixingCalculation();

        await _ebixMDR.ConfirmEnergyResultIsAvailable();
    }

    // TODO: Find ebIX actor with Wholesale data, or get Wholesale to create data for ebIX energy supplier actor 5790000610976 in grid area 543
    [Fact(Skip = "The ebIX energy supplier actor 5790000610976 (in grid area 543) does not currently have any Wholesale data")]
    public async Task Actor_can_peek_and_dequeue_wholesale_result_in_ebIX_format()
    {
        await _ebixEs.EmptyQueueForActor();

        await _calculationCompleted.PublishForWholesaleFixingCalculation();

        await _ebixEs.ConfirmWholesaleResultIsAvailable();
    }

    [Fact]
    public async Task Dequeue_request_without_content_gives_ebIX_error_B2B_900()
    {
        await _ebixMDR.ConfirmInvalidDequeueRequestGivesEbixError();
    }

    [Fact]
    public async Task Dequeue_request_with_incorrect_message_id_gives_ebIX_error_B2B_201()
    {
        await _ebixMDR.ConfirmDequeueWithIncorrectMessageIdGivesEbixError();
    }

    [Fact]
    public async Task Actor_cannot_peek_ebix_api_without_certificate()
    {
        await _ebixMDR.ConfirmPeekWithoutCertificateIsNotAllowed();
    }

    [Fact]
    public async Task Actor_cannot_dequeue_ebix_api_without_certificate()
    {
        await _ebixMDR.ConfirmDequeueWithoutCertificateIsNotAllowed();
    }

    [Fact]
    public async Task Actor_cannot_peek_when_certificate_has_been_removed()
    {
        await _actor.PublishActorCertificateCredentialsRemoved(AcceptanceTestFixture.ActorNumber, AcceptanceTestFixture.ActorRole, _ebixCertificateThumbprint);

        await _ebixMDR.ConfirmPeekWithRemovedCertificateIsNotAllowed();

        await _actor.ActorCertificateCredentialsAssigned(AcceptanceTestFixture.ActorNumber, AcceptanceTestFixture.ActorRole, _ebixCertificateThumbprint);
    }

    [Fact]
    public async Task Actor_cannot_dequeue_when_certificated_has_been_removed()
    {
        await _actor.PublishActorCertificateCredentialsRemoved(AcceptanceTestFixture.ActorNumber, AcceptanceTestFixture.ActorRole, _ebixCertificateThumbprint);

        await _ebixMDR.ConfirmDequeueWithRemovedCertificateIsNotAllowed();

        await _actor.ActorCertificateCredentialsAssigned(AcceptanceTestFixture.ActorNumber, AcceptanceTestFixture.ActorRole, _ebixCertificateThumbprint);
    }
}
