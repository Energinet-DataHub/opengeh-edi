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

using System.Net;
using WireMock;
using WireMock.Matchers;
using WireMock.Matchers.Request;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Settings;

namespace Energinet.DataHub.EDI.IntegrationTests.AuditLog.Fixture;

public sealed class AuditLogMockServer : IDisposable
{
    private const string IngestionUrlSuffix = "/auditlog";
    private readonly int _port;
    private WireMockServer? _server;

    public AuditLogMockServer(int port = 4431)
    {
        _port = port;
    }

    public string IngestionUrl => $"http://localhost:{_port}{IngestionUrlSuffix}";

    public void StartServer()
    {
        var server = WireMockServer.Start(new WireMockServerSettings
        {
            Port = _port,
        });

        MockIngestionEndpoint(server);

        _server = server;
    }

    public void Dispose()
    {
        _server?.Dispose();
    }

    public void ResetCallLogs()
    {
        _server?.ResetLogEntries();
    }

    public IReadOnlyCollection<(IRequestMessage Request, IResponseMessage Response)> GetAuditLogIngestionCalls()
    {
        var logEntries = _server?
            .FindLogEntries(
                new RequestMessagePathMatcher(
                    MatchBehaviour.AcceptOnMatch,
                    MatchOperator.And,
                    IngestionUrlSuffix));

        return logEntries?
            .Select(l => (l.RequestMessage, l.ResponseMessage))
            .ToList()
               ?? [];
    }

    private static void MockIngestionEndpoint(WireMockServer server)
    {
        var request = Request.Create()
            .WithPath(IngestionUrlSuffix)
            .UsingPost();

        var response = Response.Create()
            .WithStatusCode(HttpStatusCode.OK);

        server
            .Given(request)
            .RespondWith(response);
    }
}
