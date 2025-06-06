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

using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Energinet.DataHub.EDI.B2BApi.Configuration.Middleware;

public static class FunctionContextExtensions
{
    internal static bool IsActorProtectedEndpoint(this FunctionContext context)
    {
        var isHttpTrigger = context.FunctionDefinition.InputBindings.Values
            .First(metadata => metadata.Type.EndsWith("Trigger"))
            .Type == "httpTrigger";

        var isHealthCheckEndpoint = context.FunctionDefinition.Name == "HealthCheck";
        var isDurableFunctionMonitorEndpoint = context.FunctionDefinition.PathToAssembly.EndsWith("durablefunctionsmonitor.dotnetisolated.core.dll");

        var isSubsystemEndpoint = HasAuthorizeAttribute(context.FunctionDefinition.EntryPoint);

        return isHttpTrigger && !isHealthCheckEndpoint && !isDurableFunctionMonitorEndpoint && !isSubsystemEndpoint;
    }

    /// <summary>
    /// Sets the FunctionContext IFunctionBindingsFeature InvocationResult with a HttpResponseData.
    /// </summary>
    /// <param name="functionContext"></param>
    /// <param name="response"></param>
    internal static void SetHttpResponseData(this FunctionContext functionContext, HttpResponseData response)
    {
        var functionBindingsFeature = functionContext.GetIFunctionBindingsFeature();
        var type = functionBindingsFeature.GetType();
        var propertyInfo = type?.GetProperties().Single(p => p.Name is "InvocationResult");
        propertyInfo?.SetValue(functionBindingsFeature, response);
    }

    /// <summary>
    /// Respond with Unauthorized HTTP 401
    /// </summary>
    /// <param name="context"></param>
    /// <param name="httpRequestData"></param>
    internal static void RespondWithUnauthorized(this FunctionContext context, HttpRequestData httpRequestData)
    {
        var httpResponseData = httpRequestData.CreateResponse(HttpStatusCode.Unauthorized);
        var mediaTypeOrNull = GetMediaType(httpRequestData);

        if (mediaTypeOrNull is null)
        {
            httpResponseData.Headers.Add("Content-Type", "text/plain; charset=utf-8");
        }
        else
        {
            httpResponseData.Headers.Add(
                "Content-Type",
                mediaTypeOrNull.Contains("ebix") ? "text/xml; charset=utf-8" : $"{mediaTypeOrNull}; charset=utf-8");
        }

        context.SetHttpResponseData(httpResponseData);
    }

    /// <summary>
    /// Retrieves the IFunctionBindingsFeature property from the FunctionContext.
    /// </summary>
    /// <param name="functionContext"></param>
    /// <returns>IFunctionBindingsFeature or null</returns>
    private static object GetIFunctionBindingsFeature(this FunctionContext functionContext)
    {
        var keyValuePair = functionContext.Features.SingleOrDefault(f => f.Key.Name is "IFunctionBindingsFeature");
        return keyValuePair.Value;
    }

    private static string? GetMediaType(HttpRequestData request)
    {
        if (!request.Headers.TryGetValues("Content-Type", out var contentTypeValues))
        {
            return null;
        }

        var contentType = contentTypeValues.FirstOrDefault();
        // We assume that the media type is the first substring containing '/' of the content type header,
        // e.g. "application/json; charset=utf-8" → "application/json"
        var mediaType = contentType?.Split(';').FirstOrDefault(s => s.Contains('/'))?.Trim();

        return mediaType;
    }

    private static bool HasAuthorizeAttribute(string fullMethodName)
    {
        var methodInfo = GetMethodInfo(fullMethodName);
        if (methodInfo == null)
            return false;

        var hasAuthorizeAttribute = methodInfo.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).Any();
        return hasAuthorizeAttribute;
    }

    private static MethodInfo? GetMethodInfo(string fullMethodName)
    {
        var lastDot = fullMethodName.LastIndexOf('.');
        if (lastDot < 0) return null;

        var typeName = fullMethodName.Substring(0, lastDot);
        var methodName = fullMethodName.Substring(lastDot + 1);

        var type = Type.GetType(typeName);
        return type?.GetMethod(methodName);
    }
}
