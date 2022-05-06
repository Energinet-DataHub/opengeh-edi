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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Messaging.Api.Configuration.Middleware
{
    public static class FunctionContextExtensions
    {
        public enum TriggerType
        {
            HttpTrigger,
            TimerTrigger,
            ServiceBusTrigger,
        }

        /// <summary>
        /// Returns whether or not the <paramref name="triggerType"></paramref> is a input binding on the current context.
        /// </summary>
        internal static bool Is(this FunctionContext context, TriggerType triggerType)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            return context.FunctionDefinition.InputBindings.Any(input => input.Value.Type.Equals(triggerType.ToString(), StringComparison.OrdinalIgnoreCase));
        }

        internal static HttpRequestData? GetHttpRequestData(this FunctionContext functionContext)
        {
            if (functionContext == null) throw new ArgumentNullException(nameof(functionContext));

            var functionBindingsFeature = functionContext.GetIFunctionBindingsFeature();
            var type = functionBindingsFeature.GetType();
            var inputData = type.GetProperties().Single(p => p.Name == "InputData").GetValue(functionBindingsFeature) as IReadOnlyDictionary<string, object>;
            return inputData?.Values.SingleOrDefault(o => o is HttpRequestData) as HttpRequestData;
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
            context.SetHttpResponseData(httpResponseData);
        }

        internal static T GetService<T>(this FunctionContext functionContext)
            where T : notnull
        {
            return functionContext.InstanceServices.GetRequiredService<T>();
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
    }
}
