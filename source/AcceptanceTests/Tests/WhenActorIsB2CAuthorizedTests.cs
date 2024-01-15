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
using Xunit.Categories;

namespace Energinet.DataHub.EDI.AcceptanceTests.Tests;

[SuppressMessage("Usage", "CA2007", Justification = "Test methods should not call ConfigureAwait(), as it may bypass parallelization limits")]

[Collection(AcceptanceTestCollection.AcceptanceTestCollectionName)]
[IntegrationTest]
public sealed class WhenActorIsB2CAuthorizedTests
{
    private readonly AcceptanceTestFixture _fixture;

    public WhenActorIsB2CAuthorizedTests(AcceptanceTestFixture fixture)
    {
        _fixture = fixture;
    }

    // [Fact]
    // public async Task Actor_can_search_archived_messages()
    // {
    //     var authorizedHttpClient = await _fixture.B2CAuthorizedHttpClient;
    //
    //     using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(_fixture.B2CApiUri, "ArchivedMessageSearch"));
    //
    //     request.Content = JsonContent.Create(new { });
    //
    //     var response = await authorizedHttpClient.SendAsync(request);
    //
    //     response.EnsureSuccessStatusCode();
    //
    //     Assert.True(response.IsSuccessStatusCode);
    // }
}
