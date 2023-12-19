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
using System.Text.Json.Nodes;
using Energinet.DataHub.EDI.AcceptanceTests.Drivers;
using Energinet.DataHub.EDI.AcceptanceTests.Responses.json;
using Newtonsoft.Json.Linq;

namespace Energinet.DataHub.EDI.AcceptanceTests.Dsl;

[SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "Naming has to be sensible")]
public class ArchivedMessageDsl
{
    private readonly AzureAuthenticationDriver _azureAuthentication;
    private readonly B2CDriver _b2CDriver;

    public ArchivedMessageDsl(AzureAuthenticationDriver azureAuthentication, B2CDriver b2CDriver)
    {
        _azureAuthentication = azureAuthentication;
        _b2CDriver = b2CDriver;
    }

    internal Task<string> GetTokenForActorAsync(string username, string password)
    {
        return _azureAuthentication.GetB2CTokenAsync(username, password);
    }

    internal Task<string> ArchivedMessageGetDocumentAsync(string token, string messageId)
    {
        return _b2CDriver.ArchivedMessageGetDocumentAsync(token, messageId);
    }

    internal Task<List<ArchivedMessageSearchResponse>> RequestArchivedMessageSearchAsync(string token, JObject payload)
    {
        return _b2CDriver.RequestArchivedMessageSearchAsync(token, payload);
    }
}
