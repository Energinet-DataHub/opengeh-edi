﻿// Copyright 2020 Energinet DataHub A/S
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
using Energinet.DataHub.EDI.AcceptanceTests.Drivers;
using Energinet.DataHub.EDI.AcceptanceTests.Responses.json;
using Newtonsoft.Json.Linq;

namespace Energinet.DataHub.EDI.AcceptanceTests.Dsl;

[SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "Naming has to be sensible")]
public class ArchivedMessageDsl
{
    private readonly EdiB2CDriver _ediB2CDriver;

    public ArchivedMessageDsl(EdiB2CDriver ediB2CDriver)
    {
        _ediB2CDriver = ediB2CDriver;
    }

    internal Task<string> ArchivedMessageGetDocumentAsync(Uri requestUri)
    {
        return _ediB2CDriver.ArchivedMessageGetDocumentAsync(requestUri);
    }

    internal Task<List<ArchivedMessageSearchResponse>> RequestArchivedMessageSearchAsync(Uri requestUri, JObject payload)
    {
        return _ediB2CDriver.RequestArchivedMessageSearchAsync(requestUri, payload);
    }
}
