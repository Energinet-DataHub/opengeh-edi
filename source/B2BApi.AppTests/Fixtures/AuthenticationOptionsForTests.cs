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

namespace Energinet.DataHub.EDI.B2BApi.AppTests.Fixtures;

public static class AuthenticationOptionsForTests
{
    /// <summary>
    /// Uri (scope) for which the client must request a token and send as part of the http request.
    /// In integration tests we use one that we know all identities (developers, CI/CD) can get a token for.
    /// The protected app's use this as Audience in JWT validation.
    /// </summary>
    public const string ApplicationIdUri = "https://management.azure.com";

    /// <summary>
    /// Token issuer.
    /// The protected app's use this as Authority in JWT validation.
    /// </summary>
    public const string Issuer = "https://sts.windows.net/f7619355-6c67-4100-9a78-1847f30742e2/";
}
