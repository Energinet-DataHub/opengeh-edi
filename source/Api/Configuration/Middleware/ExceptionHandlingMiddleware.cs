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
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace Api.Configuration.Middleware;

public class ExceptionHandlingMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (next == null) throw new ArgumentNullException(nameof(next));

        try
        {
            await next(context).ConfigureAwait(false);
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            _logger.LogError(ex, "Error processing invocation");
            var httpTriggerBinding = context.FunctionDefinition.InputBindings.Values
                .FirstOrDefault(a => a.Type == "httpTrigger");

            if (httpTriggerBinding != null)
            {
                var bindingResult = await context.BindInputAsync<HttpRequestData>(httpTriggerBinding).ConfigureAwait(false);
                var i = bindingResult.Value;
            }

            var httpReqData = await context.GetHttpRequestDataAsync().ConfigureAwait(false);

            if (httpReqData != null)
            {
                // Create an instance of HttpResponseData with 500 status code.
                var newHttpResponse = httpReqData.CreateResponse(HttpStatusCode.InternalServerError);
                // You need to explicitly pass the status code in WriteAsJsonAsync method.
                // https://github.com/Azure/azure-functions-dotnet-worker/issues/776
                await newHttpResponse.WriteAsJsonAsync(
                    new { Message = "An unexpected error occurred! Please try later." },
                    newHttpResponse.StatusCode).ConfigureAwait(false);

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
    }

    private static OutputBindingData<HttpResponseData>? GetHttpOutputBindingFromMultipleOutputBinding(FunctionContext context)
    {
        // The output binding entry name will be "$return" only when the function return type is HttpResponseData
        var httpOutputBinding = context.GetOutputBindings<HttpResponseData>()
            .FirstOrDefault(b => b.BindingType == "http" && b.Name != "$return");

        return httpOutputBinding;
    }
}
