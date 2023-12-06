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

namespace Energinet.DataHub.EDI.AcceptanceTests;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2007", Justification = "Test methods should not call ConfigureAwait(), as it may bypass parallelization limits")]

[IntegrationTest]
public sealed class WhenEbixPeekRequestIsReceivedTests : TestRunner
{
    private readonly EbixRequestDsl _ebix;

    public WhenEbixPeekRequestIsReceivedTests()
    {
        _ebix = new EbixRequestDsl(
            new AzureAuthenticationDriver(AzureEntraTenantId, AzureEntraBackendAppId),
            new EdiDriver(AzpToken, ConnectionString),
            new WholesaleDriver(EventPublisher),
            new EbixDriver(new Uri(ApiManagementUri, "/ebix"), EbixCertificatePassword));
    }

    [Fact]
    public async Task Actor_can_peek_calculation_result_in_ebix_format()
    {
        await _ebix.EmptyQueueForActor(AcceptanceTestActorNumber, AcceptanceTestActorRole);
        await _ebix.PublishAggregationResultFor(AcceptanceTestActorGridArea);

        await _ebix.ConfirmPeekIsEbixFormatAndCorrectDocumentType();
    }
}
