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

using System.ComponentModel.DataAnnotations;

namespace Energinet.DataHub.EDI.B2BApi.Extensions.Options;

/// <summary>
/// Contains options for validating the JWT bearer tokens from other subsystems,
/// when they request on a http function trigger with the attribute: [Authorize] (<see cref="Microsoft.AspNetCore.Authorization.AuthorizeAttribute"/>)
/// </summary>
public class SubsystemAuthenticationOptions
{
    public const string SectionName = "SubsystemAuthentication";

    /// <summary>
    /// Uri (scope) which must match the audience of the token.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string ApplicationIdUri { get; set; } = string.Empty;

    /// <summary>
    /// Issuer (tenant) which must match the issuer of the token.
    /// Also used to configure Authority in JWT validation.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string Issuer { get; set; } = string.Empty;
}
