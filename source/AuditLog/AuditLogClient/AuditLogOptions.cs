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

namespace Energinet.DataHub.EDI.AuditLog.AuditLogServerClient;

/// <summary>
/// Required configuration options for the <see cref="AuditLogHttpClient"/>.
/// <example>
/// As an example the "AuditLog__IngestionUrl" setting must available in the configuration. The value
/// of this setting can be found in the shared Azure KeyVault under the secret name "func-log-ingestion-api-url".
/// </example>
/// </summary>
public class AuditLogOptions
{
    /// <summary>
    /// The name of the required configuration section.
    /// </summary>
    public const string SectionName = "AuditLog";

    /// <summary>
    /// The URL of the endpoint to which the audit log should be sent.
    /// <remarks>Can be found in the shared Azure KeyVault under the secret name "func-log-ingestion-api-url".</remarks>
    /// </summary>
    [Required]
    public string IngestionUrl { get; set; } = null!;
}
