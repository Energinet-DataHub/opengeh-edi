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

using Energinet.DataHub.EDI.SubsystemTests.Drivers;
using Energinet.DataHub.EDI.SubsystemTests.Drivers.Ebix;
using Energinet.DataHub.EDI.SubsystemTests.Dsl;
using Xunit.Abstractions;
using Xunit.Categories;

namespace Energinet.DataHub.EDI.SubsystemTests.Tests;

[IntegrationTest]
[Collection(SubsystemTestCollection.SubsystemTestCollectionName)]
public class WhenMeteredDataForMeteringPointIsReceivedTests : BaseTestClass
{
    private readonly MeteredDataForMeteringPointRequestDsl _meteredDataForMeteringPointGridAccessProvider;

    public WhenMeteredDataForMeteringPointIsReceivedTests(ITestOutputHelper output, SubsystemTestFixture fixture)
        : base(output, fixture)
    {
        _meteredDataForMeteringPointGridAccessProvider = new MeteredDataForMeteringPointRequestDsl(
            new EbixDriver(
                fixture.EbixUri,
                fixture.EbixGridAccessProviderCredentials,
                output),
            new EdiDriver(
                fixture.DurableClient,
                fixture.B2BClients.GridAccessProvider,
                output),
            new EdiDatabaseDriver(fixture.ConnectionString));
    }

    [Fact]
    public async Task Actor_can_send_metered_data_for_metering_point_in_cim_to_datahub()
    {
        var messageId = await _meteredDataForMeteringPointGridAccessProvider
            .SendMeteredDataForMeteringPointInCimAsync(CancellationToken.None);

        await _meteredDataForMeteringPointGridAccessProvider.ConfirmRequestIsReceivedAsync(
            messageId,
            CancellationToken.None);
    }

    [Fact(Skip = "Ebix is not supported.")]
    public async Task Actor_can_send_metered_data_for_metering_point_in_ebix_to_datahub()
    {
        var messageId = await _meteredDataForMeteringPointGridAccessProvider
            .SendMeteredDataForMeteringPointInEbixAsync(CancellationToken.None);

        await _meteredDataForMeteringPointGridAccessProvider.ConfirmRequestIsReceivedAsync(
            messageId,
            CancellationToken.None);
    }

    [Fact(Skip = "Ebix is not supported.")]
    public async Task Actor_sends_metered_data_for_metering_point_in_ebix_with_already_used_message_id_to_datahub()
    {
        var faultMessage = await _meteredDataForMeteringPointGridAccessProvider
            .SendMeteredDataForMeteringPointInEbixWithAlreadyUsedMessageIdAsync(CancellationToken.None);

        var expectedErrorMessage = "B2B-003:The provided Ids are not unique and have been used before";

        _meteredDataForMeteringPointGridAccessProvider.ConfirmResponseContainsValidationError(
            faultMessage,
            expectedErrorMessage,
            CancellationToken.None);
    }
}
