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

using System.Net;
using Energinet.DataHub.EDI.SystemTests.Drivers;
using Energinet.DataHub.EDI.SystemTests.Models;
using Xunit;

namespace Energinet.DataHub.EDI.SystemTests.Dsl;

internal sealed class AuthenticationTokenRequestDsl
{
    private readonly EdiDriver _ediDriver;

    internal AuthenticationTokenRequestDsl(EdiDriver ediDriver)
    {
        _ediDriver = ediDriver;
    }

    internal async Task ConfirmRequestAggregatedMeasureDataWithoutTokenIsNotAllowedAsync(
        CancellationToken cancellationToken)
    {
        var act = async () =>
        {
            await _ediDriver
                .SendRequestAsync(
                    actor: null,
                    MessageType.RequestAggregatedMeasureData,
                    cancellationToken)
                .ConfigureAwait(false);
        };

        var httpRequestException = await Assert.ThrowsAsync<HttpRequestException>(act).ConfigureAwait(false);

        Assert.Equal(HttpStatusCode.Unauthorized, httpRequestException.StatusCode);
    }

    internal async Task ConfirmPeekWithoutTokenIsNotAllowedAsync(CancellationToken cancellationToken)
    {
        var act = async () =>
        {
            await _ediDriver.PeekAsync(actor: null, cancellationToken).ConfigureAwait(false);
        };

        var httpRequestException = await Assert.ThrowsAsync<HttpRequestException>(act).ConfigureAwait(false);

        Assert.Equal(HttpStatusCode.Unauthorized, httpRequestException.StatusCode);
    }

    internal async Task ConfirmDequeueWithoutTokenIsNotAllowedAsync(CancellationToken cancellationToken)
    {
        var act = async () =>
        {
            await _ediDriver.DequeueAsync(actor: null, "irrelevant-message-id", cancellationToken)
                .ConfigureAwait(false);
        };

        var httpRequestException = await Assert.ThrowsAsync<HttpRequestException>(act).ConfigureAwait(false);

        Assert.Equal(HttpStatusCode.Unauthorized, httpRequestException.StatusCode);
    }
}
