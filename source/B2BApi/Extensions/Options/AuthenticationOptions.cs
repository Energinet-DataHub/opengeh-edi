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
/// Contains options for validating the JWT bearer tokens that must be sent as
/// part of any http request for protected http endpoints.
/// </summary>
public class AuthenticationOptions
{
    public const string SectionName = "Auth";

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
