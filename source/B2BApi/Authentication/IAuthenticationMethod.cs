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

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Http;

namespace Energinet.DataHub.EDI.B2BApi.Authentication;

/// <summary>
/// Authentication method for handling user authentication in http requests
/// </summary>
public interface IAuthenticationMethod
{
    /// <summary>
    /// Check if the authentication method should handle user authentication for the http request
    /// </summary>
    /// <param name="httpRequestData"></param>
    bool ShouldHandle(HttpRequestData httpRequestData);

    /// <summary>
    /// Authenticates the user for the http request
    /// </summary>
    Task<bool> AuthenticateAsync(HttpRequestData httpRequestData, CancellationToken cancellationToken);
}
