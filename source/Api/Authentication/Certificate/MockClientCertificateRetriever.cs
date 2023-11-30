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

using System;
using System.Security.Cryptography.X509Certificates;
using Energinet.DataHub.EDI.Api.Configuration;
using Microsoft.Azure.Functions.Worker.Http;

namespace Energinet.DataHub.EDI.Api.Authentication.Certificate;

public class MockClientCertificateRetriever : IClientCertificateRetriever
{
    public X509Certificate2? GetCertificate(HttpRequestData httpRequestData)
    {
        ArgumentNullException.ThrowIfNull(httpRequestData);

        var certificate = new MockCertificate("12345");

        return certificate;
    }
}
