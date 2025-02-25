using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours.IncomingRequests;

public sealed class GivenMeteredDataForMeteringPointV2Tests(
    IntegrationTestFixture integrationTestFixture,
    ITestOutputHelper testOutputHelper)
    : MeteredDataForMeteringPointBehaviourTestBase(integrationTestFixture, testOutputHelper)
{

}
