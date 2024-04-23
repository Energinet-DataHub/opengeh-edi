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

using Energinet.DataHub.EDI.SystemTests.Drivers;
using Energinet.DataHub.EDI.SystemTests.Models;

namespace Energinet.DataHub.EDI.SystemTests.Dsl;

internal sealed class DatahubDsl
{
    private readonly EdiDriver _ediDriver;

    internal DatahubDsl(EdiDriver ediDriver)
    {
        _ediDriver = ediDriver;
    }

    internal async Task EmptyQueueForAsync(Actor actor, CancellationToken cancellationToken)
    {
        var peekResponse = await _ediDriver.PeekAsync(actor, cancellationToken).ConfigureAwait(false);
        while (peekResponse.Headers.TryGetValues("MessageId", out var messageIds))
        {
            await _ediDriver.DequeueAsync(actor, messageIds.First(), cancellationToken).ConfigureAwait(false);
            peekResponse = await _ediDriver.PeekAsync(actor, cancellationToken).ConfigureAwait(false);
        }
    }
}
