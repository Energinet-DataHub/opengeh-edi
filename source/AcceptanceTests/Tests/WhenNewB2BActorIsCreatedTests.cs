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

using System.Diagnostics.CodeAnalysis;
using Energinet.DataHub.EDI.AcceptanceTests.Drivers;
using Energinet.DataHub.EDI.AcceptanceTests.Dsl;
using Xunit.Categories;
#pragma warning disable CS0162 // Unreachable code detected

namespace Energinet.DataHub.EDI.AcceptanceTests.Tests;

[SuppressMessage("Usage", "CA2007", Justification = "Test methods should not call ConfigureAwait(), as it may bypass parallelization limits")]
[IntegrationTest]
[Collection(AcceptanceTestCollection.AcceptanceTestCollectionName)]
[SuppressMessage("ReSharper", "HeuristicUnreachableCode", Justification = "Unreachable code is disabled for local executions")]
public sealed class WhenNewB2BActorIsCreatedTests
{
    private readonly ActorDsl _actorDsl;

    public WhenNewB2BActorIsCreatedTests(AcceptanceTestFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        _actorDsl = new ActorDsl(
            new MarketParticipantDriver(fixture.EventPublisher),
            new EdiActorDriver(
                fixture.ConnectionString));
    }

    [Fact]
    public async Task Actor_is_created()
    {
#if DEBUG
        return; // skipped locally, b2c api is not public available
#endif
        var b2CId = Guid.NewGuid().ToString();
        await _actorDsl.PublishResult(actorNumber: "8880000610888", b2CId: b2CId);

        var exists = await _actorDsl.ConfirmActorIsAvailable(actorNumber: "8880000610888", b2CId: b2CId);
        Assert.True(exists);
    }
}
