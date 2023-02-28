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

using AcceptanceTest.Fixtures;
using Energinet.DataHub.Core.FunctionApp.TestCommon;
using Xunit.Abstractions;
using Xunit.Categories;

namespace AcceptanceTest;

[IntegrationTest]
[Collection(nameof(TestCommonCollectionFixture))]
public class TestCommonTests : IAsyncLifetime
{
    private readonly TestCommonHostFixture _fixture;

    public TestCommonTests(TestCommonHostFixture fixture, ITestOutputHelper testOutputHelper)
    {
        _fixture = fixture;
        _fixture.SetTestOutputHelper(testOutputHelper);
        _fixture.App01HostManager.ClearHostLog();
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _fixture.SetTestOutputHelper(null!);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task<int> Dummy_test()
    {
        return await Task.FromResult(0).ConfigureAwait(false);
    }
}
