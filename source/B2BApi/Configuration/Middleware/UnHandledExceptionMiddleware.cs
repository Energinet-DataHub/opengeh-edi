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
using Energinet.DataHub.EDI.B2BApi.Common;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.B2BApi.Configuration.Middleware;

/// <summary>
/// Ensure we dont leak exception details for http triggers.
/// </summary>
/// <remarks>Inspired by https://github.com/Azure/azure-functions-dotnet-worker/blob/main/samples/CustomMiddleware/ExceptionHandlingMiddleware.cs</remarks>
public sealed class UnHandledExceptionMiddleware(ILogger<UnHandledExceptionMiddleware> logger)
    : IFunctionsWorkerMiddleware
{
    // DO NOT inject scoped services in the middleware constructor.
    // DO use scoped services in middleware by retrieving them from 'FunctionContext.InstanceServices'
    // DO NOT store scoped services in fields or properties of the middleware object. See https://github.com/Azure/azure-functions-dotnet-worker/issues/1327#issuecomment-1434408603
    private const string ErrorMessage = "An unexpected error occurred! Please try later.";
    private readonly ILogger _logger = logger;

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        try
        {
            await next(context);
        }
        catch (OperationCanceledException operationCanceledException)
        {
            // This catch block handles cancellations triggered by a CancellationToken.
            // E.g. if a task is cancelled it throws a TaskCanceledException which is a OperationCanceledException.
            // E.g. if cancellationToken.ThrowIfCancellationRequested() it throws an OperationCanceledException.
            // It logs a warning message indicating that the request was cancelled.
            _logger.LogWarning(operationCanceledException, "Request was cancelled: {Ex}", operationCanceledException.Message);
        }
        catch (Exception ex)
        {
            // This catch block handles all other exceptions.
            // It logs an error message indicating that an error occurred during the invocation.
            _logger.LogError(ex, "Error processing invocation: {Ex}", ex.Message);

            // Check if request has been disposed before accessing headers
            // This can happen if the request is disposed before the function is executed
            // E.g. if the request is cancelled by the client
            if (HttpContextHasBeenDisposed(context))
            {
                return;
            }

            var httpReqData = await context.GetHttpRequestDataAsync();
            if (httpReqData == null)
            {
                return;
            }

            // Create an instance of HttpResponseData with 500 status code.
            var newHttpResponse = await CreateHttpResponseAsync(httpReqData);

            var invocationResult = context.GetInvocationResult();

            var httpOutputBindingFromMultipleOutputBindings = GetHttpOutputBindingFromMultipleOutputBinding(context);
            if (httpOutputBindingFromMultipleOutputBindings is not null)
            {
                httpOutputBindingFromMultipleOutputBindings.Value = newHttpResponse;
            }
            else
            {
                invocationResult.Value = newHttpResponse;
            }
        }
    }

    private static bool HttpContextHasBeenDisposed(FunctionContext context)
    {
        try
        {
            return context.GetHttpContext()?.Features is null;
        }
        catch (ObjectDisposedException)
        {
            return true;
        }
    }

    private static async Task<HttpResponseData> CreateHttpResponseAsync(HttpRequestData httpReqData)
    {
        var newHttpResponse = httpReqData.CreateResponse(HttpStatusCode.InternalServerError);

        var contentType = httpReqData.Headers.TryGetContentType();

        if (contentType is not null && contentType.Contains("application/json"))
        {
            await CreateJsonResponseAsync(newHttpResponse);
        }
        else if (contentType is not null && contentType.Contains("application/xml"))
        {
            await CreateXmlResponseAsync(newHttpResponse);
        }
        else
        {
            await CreatePlainTextResponseAsync(newHttpResponse);
        }

        return newHttpResponse;
    }

    private static async Task CreatePlainTextResponseAsync(HttpResponseData newHttpResponse)
    {
        newHttpResponse.Headers.Add("Content-Type", "text/plain; charset=utf-8");
        await newHttpResponse.WriteStringAsync(ErrorMessage);
    }

    private static async Task CreateXmlResponseAsync(HttpResponseData newHttpResponse)
    {
        newHttpResponse.Headers.Add("Content-Type", "application/xml; charset=utf-8");
        await newHttpResponse.WriteStringAsync(
            $"""
             <Error>
                 <Message>{ErrorMessage}</Message>
             </Error>
             """);
    }

    private static async Task CreateJsonResponseAsync(HttpResponseData newHttpResponse)
    {
        // The method ensures that the content type is "application/json; charset=utf-8"
        await newHttpResponse.WriteAsJsonAsync(new { Message = ErrorMessage });
    }

    private static OutputBindingData<HttpResponseData>? GetHttpOutputBindingFromMultipleOutputBinding(FunctionContext context)
    {
        // The output binding entry name will be "$return" only when the function return type is HttpResponseData
        var httpOutputBinding = context.GetOutputBindings<HttpResponseData>()
            .FirstOrDefault(b => b.BindingType == "http" && b.Name != "$return");

        return httpOutputBinding;
    }
}
