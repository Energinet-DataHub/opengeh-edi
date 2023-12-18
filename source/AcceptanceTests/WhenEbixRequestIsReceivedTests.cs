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
using Energinet.DataHub.EDI.AcceptanceTests.Drivers;
using Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix;
using Energinet.DataHub.EDI.AcceptanceTests.Dsl;
using Energinet.DataHub.EDI.AcceptanceTests.Tests;
using Xunit.Categories;

namespace Energinet.DataHub.EDI.AcceptanceTests;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2007", Justification = "Test methods should not call ConfigureAwait(), as it may bypass parallelization limits")]

[IntegrationTest]
[Collection(TestRunner.AcceptanceTestCollection)]
public sealed class WhenEbixRequestIsReceivedTests
{
    private readonly EbixRequestDsl _ebix;
    private readonly TestRunner _runner;

    public WhenEbixRequestIsReceivedTests(TestRunner runner)
    {
        Debug.Assert(runner != null, nameof(runner) + " != null");
        _runner = runner;
        _ebix = new EbixRequestDsl(
            new AzureAuthenticationDriver(_runner.AzureEntraTenantId, _runner.AzureEntraBackendAppId),
            new EdiDriver(_runner.AzpToken, _runner.ConnectionString),
            new WholesaleDriver(_runner.EventPublisher),
            new EbixDriver(new Uri(_runner.ApiManagementUri, "/ebix"), runner.EbixCertificatePassword));
    }

    [Fact]
    public async Task Actor_can_peek_and_dequeue_aggregation_result_in_ebIX_format()
    {
        await _ebix.EmptyQueueForActor(TestRunner.ActorNumber, TestRunner.ActorRole);
        await _ebix.PublishAggregationResultFor(TestRunner.ActorGridArea);

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
}
