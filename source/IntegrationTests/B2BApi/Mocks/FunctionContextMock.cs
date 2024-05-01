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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Energinet.DataHub.Core.App.FunctionApp;
using Energinet.DataHub.EDI.B2BApi.Authentication.Certificate;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Energinet.DataHub.EDI.IntegrationTests.B2BApi.Mocks;

/// <summary>
/// A mock of FunctionContext, for use in tests where the FunctionContext is needed (an example is testing functions middleware). Should be build using the FunctionContextBuilder class. Build to support testing the MarketActorAuthenticatorMiddleware, so it probably must be expanded to support other cases as well.
/// - Only supports triggered by HTTP, and can set a content type, bearer token and/or a client request certificate.
/// - Must use the custom GetHttpResponse() function to get the HttpResponseData. The build in extension method GetHttpResponseData() does not work using the mock, since the implementation in the official Azure Function package expects an instance of IFunctionBindingsFeature which is internal.
/// </summary>
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Test class")]
internal sealed class FunctionContextMock : FunctionContext
{
    public FunctionContextMock(IServiceProvider serviceProvider, TriggerType triggerType, string? functionName, string? contentType, string? bearerToken, string? certificateHexString)
    {
        InstanceServices = serviceProvider;

        var mockHttpRequestData = new MockHttpRequestData(this);

        if (!string.IsNullOrWhiteSpace(contentType))
            mockHttpRequestData.Headers.Add("Content-Type", contentType);

        if (!string.IsNullOrWhiteSpace(bearerToken))
            mockHttpRequestData.Headers.Add("Authorization", $"Bearer {bearerToken}");

        if (!string.IsNullOrWhiteSpace(certificateHexString))
            mockHttpRequestData.Headers.Add(HeaderClientCertificateRetriever.CertificateHeaderName, certificateHexString);

        Features.Set<IHttpRequestDataFeature>(new MockHttpRequestDataFeature(mockHttpRequestData));
        Features.Set<IFunctionBindingsFeature>(new MockFunctionBindingsFeature());

        FunctionDefinition = new MockFunctionDefinition(triggerType, functionName);
    }

    public override string InvocationId { get; } = null!;

    public override string FunctionId { get; } = null!;

    public override TraceContext TraceContext { get; } = null!;

    public override BindingContext BindingContext { get; } = null!;

    public override RetryContext RetryContext { get; } = null!;

    public override IServiceProvider InstanceServices { get; set; }

    public override FunctionDefinition FunctionDefinition { get; }

    public override IDictionary<object, object> Items { get; set; } = null!;

    public override IInvocationFeatures Features { get; } = new MockFeatures();

    public HttpResponseData? GetHttpResponse()
    {
        var functionBindingsFeature = Features.Get<IFunctionBindingsFeature>();
        return (HttpResponseData?)functionBindingsFeature?.InvocationResult;
    }

    private sealed class MockFeatures : IInvocationFeatures
    {
        private readonly Dictionary<Type, object> _features = new();

        public IEnumerator<KeyValuePair<Type, object>> GetEnumerator()
        {
            return _features.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Set<T>(T? instance)
        {
            var type = typeof(T);

            if (instance == null)
                _features.Remove(type);
            else
                _features[type] = instance;
        }

        public T? Get<T>()
        {
            return (T?)_features.GetValueOrDefault(typeof(T));
        }
    }

    private sealed class MockHttpRequestDataFeature : IHttpRequestDataFeature
    {
        private readonly HttpRequestData _httpRequestData;

        public MockHttpRequestDataFeature(MockHttpRequestData httpRequestData)
        {
            _httpRequestData = httpRequestData;
        }

        public ValueTask<HttpRequestData?> GetHttpRequestDataAsync(FunctionContext context)
        {
            var isHttpTrigger = context.FunctionDefinition.InputBindings.Any(
                input => string.Equals(
                    input.Value.Type,
                    TriggerType.HttpTrigger.ToString(),
                    StringComparison.OrdinalIgnoreCase));

            var httpRequestData = isHttpTrigger ? _httpRequestData : null;
            return new ValueTask<HttpRequestData?>(httpRequestData);
        }
    }

    private sealed class MockHttpRequestData : HttpRequestData
    {
        public MockHttpRequestData(FunctionContext functionContext)
            : base(functionContext)
        {
        }

        public override Stream Body { get; } = null!;

        public override HttpHeadersCollection Headers { get; } = new();

        public override IReadOnlyCollection<IHttpCookie> Cookies { get; } = null!;

        public override Uri Url { get; } = null!;

        public override IEnumerable<ClaimsIdentity> Identities { get; } = null!;

        public override string Method { get; } = null!;

        public override HttpResponseData CreateResponse()
        {
            return new MockHttpResponseData(FunctionContext);
        }
    }

    private sealed class MockHttpResponseData : HttpResponseData
    {
        public MockHttpResponseData(FunctionContext functionContext)
            : base(functionContext)
        {
        }

        public override HttpStatusCode StatusCode { get; set; }

        public override HttpHeadersCollection Headers { get; set; } = new();

        public override Stream Body { get; set; } = null!;

        public override HttpCookies Cookies { get; } = null!;
    }

    private sealed class MockFunctionDefinition : FunctionDefinition
    {
        public MockFunctionDefinition(TriggerType triggerType, string? name)
        {
            var dictionary = new Dictionary<string, BindingMetadata>
            {
                { "Trigger", new MockBindingMetadata(triggerType.ToString(), triggerType.ToString(), BindingDirection.In) },
            };
            InputBindings = ImmutableDictionary.CreateRange(dictionary);
            Parameters = ImmutableArray<FunctionParameter>.Empty;
            Name = name ?? null!;
        }

        public override ImmutableArray<FunctionParameter> Parameters { get; }

        public override string Name { get; }

        public override string PathToAssembly => null!;

        public override string EntryPoint => null!;

        public override string Id => null!;

        public override IImmutableDictionary<string, BindingMetadata> InputBindings { get; }

        public override IImmutableDictionary<string, BindingMetadata> OutputBindings => null!;
    }

    private sealed class MockBindingMetadata : BindingMetadata
    {
        public MockBindingMetadata(string name, string type, BindingDirection direction)
        {
            Name = name;
            Type = type;
            Direction = direction;
        }

        public override string Name { get; }

        public override string Type { get; }

        public override BindingDirection Direction { get; }
    }

    private sealed class MockFunctionBindingsFeature : IFunctionBindingsFeature
    {
        public object? InvocationResult { get; set; }
    }
}
