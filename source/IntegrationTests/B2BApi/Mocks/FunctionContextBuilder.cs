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

namespace Energinet.DataHub.EDI.IntegrationTests.B2BApi.Mocks;

/// <summary>
/// Builds a mock of a function context, for use in tests where an Azure Function's FunctionContext is needed (an example is testing functions middleware).
/// - Supports triggered by HTTP, and needs to be expanded if we want to test something like ServiceBus triggers.
/// </summary>
internal sealed class FunctionContextBuilder
{
    private readonly IServiceProvider _serviceProvider;
    private TriggerType _triggerType = TriggerType.HttpTrigger;
    private string? _contentType = "application/json";
    private string? _bearerToken;
    private X509Certificate2? _certificate;
    private string? _functionName;

    public FunctionContextBuilder(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Set the function context to be triggered by HTTP, to simulate an Azure Function that uses a HttpTrigger
    /// </summary>
    /// <param name="withContentType">Sets the mock HTTP request content type to the specified value</param>
    /// <param name="withToken">If not null, sets the mock HTTP request's Authorization header to the specified bearer token</param>
    /// <param name="withCertificate">If not null, sets the mock HTTP request's ClientCert header to a raw hex string representing the certificate</param>
    public FunctionContextBuilder TriggeredByHttp(string? withContentType = null, string? withToken = null, X509Certificate2? withCertificate = null)
    {
        _triggerType = TriggerType.HttpTrigger;
        _contentType = withContentType;
        _bearerToken = withToken;
        _certificate = withCertificate;

        return this;
    }

    /// <summary>
    /// Set the function context to be triggered by the given type, to simulate an Azure function that uses the given trigger type
    /// </summary>
    public FunctionContextBuilder WithTriggeredBy(TriggerType triggerType)
    {
        _triggerType = triggerType;

        return this;
    }

    /// <summary>
    /// Set the function name, which simulates the name given to the Azure Function (ie. PeekRequestListener, HealthCheck, TenSecondsHasPassed etc.)
    /// </summary>
    public FunctionContextBuilder WithFunctionName(string functionName)
    {
        _functionName = functionName;

        return this;
    }

    internal FunctionContextMock Build()
    {
        return new FunctionContextMock(
            _serviceProvider,
            _triggerType,
            _functionName,
            _contentType,
            _bearerToken,
            _certificate?.GetRawCertDataString());
    }
}
