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

using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NodaTime;
using NodaTime.Text;

namespace Energinet.DataHub.EDI.AuditLog.AuditLogClient;

/// <inheritdoc />
internal class AuditLogHttpClient(
    IHttpClientFactory httpClientFactory,
    IOptions<AuditLogOptions> auditLogOptions,
    ILogger<AuditLogHttpClient> logger) : IAuditLogClient
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ILogger _logger = logger;
    private readonly AuditLogOptions _auditLogOptions = auditLogOptions.Value;

    /// <inheritdoc />
    /// <exception cref="HttpRequestException">
    /// Thrown if performing the audit log HTTP request failed. The request will also be logged as a warning
    /// </exception>
    public async Task LogAsync(
        Guid logId,
        Guid userId,
        Guid actorId,
        Guid systemId,
        string? permissions,
        Instant occuredOn,
        string activity,
        string origin,
        object? payload,
        string? affectedEntityType,
        string? affectedEntityKey)
    {
        var payloadAsJson = payload switch
        {
            null => string.Empty,
            string p => p,
            _ => JsonSerializer.Serialize(payload),
        };

        var requestContent = new AuditLogRequestBody(
            logId,
            userId,
            actorId,
            systemId,
            permissions,
            InstantPattern.General.Format(occuredOn),
            activity,
            origin,
            payloadAsJson,
            affectedEntityType ?? string.Empty,
            affectedEntityKey ?? string.Empty);

        var httpClient = _httpClientFactory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, _auditLogOptions.IngestionUrl);
        using var requestStringContent = new StringContent(
            JsonSerializer.Serialize(requestContent),
            Encoding.UTF8,
            "application/json");
        request.Content = requestStringContent;

        using var response = await httpClient.SendAsync(request)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            string stringContentToLog;
            try
            {
                stringContentToLog = await requestStringContent.ReadAsStringAsync()
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.LogWarning(
                    e,
                    "Failed to read request content as string after audit log failed. Attempted request content: {RequestContent}",
                    requestContent);

                stringContentToLog = $"<Failed to read request content as string with exception message: {e.Message}>";
            }

            var headersToLog = string.Join(
                ", ",
                request.Content.Headers
                    .Select(h =>
                        $"{h.Key}: {string.Join(", ", h.Value.Select(value => $"\"{value}\""))}"));

            _logger.LogWarning(
                "Failed to log audit log entry. Response status code: {StatusCode}."
                + " Request headers: {RequestHeaders}"
                + " Request content as string:\n{RequestContent}",
                response.StatusCode,
                headersToLog,
                stringContentToLog);
        }

        response.EnsureSuccessStatusCode();
    }
}
