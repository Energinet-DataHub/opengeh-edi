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
using FluentAssertions;

namespace Energinet.DataHub.EDI.AcceptanceTests.Dsl;

internal sealed class NotifyWholesaleServicesDsl
{
    private readonly WholesaleDriver _wholesaleDriver;
    private readonly EdiDriver _ediDriver;

    #pragma warning disable VSTHRD200 // Since this is a DSL we don't want to suffix tasks with 'Async' since it is not part of the ubiquitous language

    internal NotifyWholesaleServicesDsl(EdiDriver ediDriverDriver, WholesaleDriver wholesaleDriverDriver)
    {
        _ediDriver = ediDriverDriver;
        _wholesaleDriver = wholesaleDriverDriver;
    }

    internal async Task<string> ConfirmResultIsAvailable()
    {
        var peekResponse = await _ediDriver.PeekMessageAsync().ConfigureAwait(false);
        var messageId = peekResponse.Headers.GetValues("MessageId").FirstOrDefault();
        var contentString = await peekResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

        messageId.Should().NotBeNull();
        contentString.Should().NotBeNull();
        contentString.Should().Contain("NotifyWholesaleServices_MarketDocument");
        return messageId!;
    }

    internal async Task ConfirmRejectResultIsAvailable()
    {
        var peekResponse = await _ediDriver.PeekMessageAsync().ConfigureAwait(false);
        var messageId = peekResponse.Headers.GetValues("MessageId").FirstOrDefault();
        var contentString = await peekResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

        messageId.Should().NotBeNull();
        contentString.Should().NotBeNull();
        contentString.Should().Contain("RejectRequestWholesaleSettlement_MarketDocument");
    }
}
