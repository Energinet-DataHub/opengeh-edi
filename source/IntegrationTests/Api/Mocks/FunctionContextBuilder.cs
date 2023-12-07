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
using Energinet.DataHub.Core.App.FunctionApp;

namespace Energinet.DataHub.EDI.IntegrationTests.Api.Mocks;

internal sealed class FunctionContextBuilder
{
    private readonly IServiceProvider _serviceProvider;
    private TriggerType _triggerType = TriggerType.HttpTrigger;
    private string? _contentType = "application/json";
    private string? _bearerToken;
    private X509Certificate2? _certificate;

    public FunctionContextBuilder(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public FunctionContextBuilder TriggeredByHttp(string? withContentType, string? withToken = null, X509Certificate2? withCertificate = null)
    {
        _triggerType = TriggerType.HttpTrigger;
        _contentType = withContentType;
        _bearerToken = withToken;
        _certificate = withCertificate;

        return this;
    }

    internal FunctionContextMock Build()
    {
        return new FunctionContextMock(
            _serviceProvider,
            _triggerType,
            _contentType,
            _bearerToken,
            _certificate?.GetRawCertDataString());
    }
}
