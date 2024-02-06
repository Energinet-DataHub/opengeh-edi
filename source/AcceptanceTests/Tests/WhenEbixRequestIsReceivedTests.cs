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

using Energinet.DataHub.EDI.AcceptanceTests.Drivers;
using Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix;
using Energinet.DataHub.EDI.AcceptanceTests.Dsl;
using Xunit.Categories;

namespace Energinet.DataHub.EDI.AcceptanceTests.Tests;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2007", Justification = "Test methods should not call ConfigureAwait(), as it may bypass parallelization limits")]

[IntegrationTest]
[Collection(AcceptanceTestCollection.AcceptanceTestCollectionName)]
public sealed class WhenEbixPeekRequestIsReceivedTests
{
    private readonly EbixRequestDsl _ebix;
    private readonly AcceptanceTestFixture _fixture;
    private readonly ActorDsl _actor;

    public WhenEbixPeekRequestIsReceivedTests(AcceptanceTestFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        _fixture = fixture;

        _ebix = new EbixRequestDsl(
            new WholesaleDriver(fixture.EventPublisher),
            new EbixDriver(new Uri(fixture.ApiManagementUri, "/ebix"), fixture.EbixCertificatePassword));
        _actor = new ActorDsl(new MarketParticipantDriver(fixture.EventPublisher), new EdiActorDriver(
            fixture.ConnectionString));
    }

    [Fact]
    public async Task Actor_can_peek_and_dequeue_aggregation_result_in_ebIX_format()
    {
        await _ebix.EmptyQueueForActor();

        await _ebix.PublishAggregationResultFor(AcceptanceTestFixture.EbixActorGridArea);

        await _ebix.ConfirmEbixResultIsAvailableForActor();
    }

    [Fact]
    public async Task Dequeue_request_without_content_gives_ebIX_error_B2B_900()
    {
        await _ebix.ConfirmInvalidDequeueRequestGivesEbixError();
    }

    [Fact]
    public async Task Dequeue_request_with_incorrect_message_id_gives_ebIX_error_B2B_201()
    {
        await _ebix.ConfirmDequeueWithIncorrectMessageIdGivesEbixError();
    }

    [Fact]
    public async Task Actor_cannot_peek_ebix_api_without_certificate()
    {
        await _ebix.ConfirmPeekWithoutCertificateIsNotAllowed();
    }

    [Fact]
    public async Task Actor_cannot_dequeue_ebix_api_without_certificate()
    {
        await _ebix.ConfirmDequeueWithoutCertificateIsNotAllowed();
    }

    [Fact]
    public async Task Actor_cannot_peek_when_certificate_has_been_removed()
    {
        await _actor.PublishActorCertificateCredentialsRemovedForAsync(AcceptanceTestFixture.ActorNumber, AcceptanceTestFixture.ActorRole, _fixture.EbixCertificateThumbprint);

        await _ebix.ConfirmPeekWithRemovedCertificateIsNotAllowed();

        await _actor.ActorCertificateCredentialsAssignedAsync(AcceptanceTestFixture.ActorNumber, AcceptanceTestFixture.ActorRole, _fixture.EbixCertificateThumbprint);
    }

    [Fact]
    public async Task Actor_cannot_dequeue_when_certificated_has_been_removed()
    {
        await _actor.PublishActorCertificateCredentialsRemovedForAsync(AcceptanceTestFixture.ActorNumber, AcceptanceTestFixture.ActorRole, _fixture.EbixCertificateThumbprint);

        await _ebix.ConfirmDequeueWithRemovedCertificateIsNotAllowed();

        await _actor.ActorCertificateCredentialsAssignedAsync(AcceptanceTestFixture.ActorNumber, AcceptanceTestFixture.ActorRole, _fixture.EbixCertificateThumbprint);
    }
}
