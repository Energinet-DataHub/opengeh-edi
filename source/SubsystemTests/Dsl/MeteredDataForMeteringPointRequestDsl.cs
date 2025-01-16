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

using System.Xml;
using Energinet.DataHub.EDI.SubsystemTests.Drivers;
using Energinet.DataHub.EDI.SubsystemTests.Drivers.Ebix;
using FluentAssertions;

namespace Energinet.DataHub.EDI.SubsystemTests.Dsl;

internal sealed class MeteredDataForMeteringPointRequestDsl(EbixDriver ebix, EdiDriver ediDriver, EdiDatabaseDriver ediDatabaseDriver)
{
    private readonly EbixDriver _ebix = ebix;
    private readonly EdiDriver _ediDriver = ediDriver;
    private readonly EdiDatabaseDriver _ediDatabaseDriver = ediDatabaseDriver;

    public async Task<string> SendMeteredDataForMeteringPointInEbixAsync(CancellationToken cancellationToken)
    {
        return await _ebix.SendMeteredDataForMeteringPointAsync(cancellationToken);
    }

    public async Task ConfirmRequestIsReceivedAsync(string messageId, CancellationToken cancellationToken)
    {
        var processId = await _ediDatabaseDriver
            .GetMeteredDataForMeteringPointProcessIdAsync(messageId, cancellationToken)
            .ConfigureAwait(false);

        processId.Should().NotBeNull("because the metered data for metering point process should be initialized");
    }

    public async Task<string> SendMeteredDataForMeteringPointInEbixWithAlreadyUsedMessageIdAsync(CancellationToken cancellationToken)
    {
        return await _ebix
            .SendMeteredDataForMeteringPointWithAlreadyUsedMessageIdAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public void ConfirmResponseContainsValidationError(string response, string errorMessage, CancellationToken none)
    {
        response.Should().BeEquivalentTo(errorMessage);
    }

    public async Task<string> SendMeteredDataForMeteringPointInCimAsync(CancellationToken cancellationToken)
    {
        return await _ediDriver.SendMeteredDataForMeteringPointAsync(cancellationToken);
    }
}
