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

using Energinet.DataHub.EDI.SystemTests.Dsl;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.EDI.SystemTests.Tests;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2007", Justification = "Test methods should not call ConfigureAwait(), as it may bypass parallelization limits")]

[IntegrationTest]
[Collection(SystemTestCollection.SystemTestCollectionName)]
public sealed class WhenAuthenticationTokenIsMissingTests
{
    private readonly SystemTestFixture _fixture;
    private readonly AuthenticationTokenRequestDsl _authenticationTokenRequest;

    public WhenAuthenticationTokenIsMissingTests(SystemTestFixture fixture)
    {
        _fixture = fixture;
        ArgumentNullException.ThrowIfNull(fixture);
        _authenticationTokenRequest = new AuthenticationTokenRequestDsl(_fixture.EdiDriver);
    }

    [Fact]
    public async Task Actor_cannot_request_aggregated_measure_data_without_token()
    {
        await _authenticationTokenRequest.ConfirmRequestAggregatedMeasureDataWithoutTokenIsNotAllowedAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Actor_cannot_peek_without_token()
    {
        await _authenticationTokenRequest.ConfirmPeekWithoutTokenIsNotAllowedAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Actor_cannot_dequeue_without_token()
    {
        await _authenticationTokenRequest.ConfirmDequeueWithoutTokenIsNotAllowedAsync(CancellationToken.None);
    }
}
