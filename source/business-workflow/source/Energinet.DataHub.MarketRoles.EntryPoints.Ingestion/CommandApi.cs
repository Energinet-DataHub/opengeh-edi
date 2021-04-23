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
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketRoles.EntryPoints.Ingestion
{
    public static class CommandApi
    {
        [Function("CommandApi")]
        public static async Task<HttpResponseData> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData request,
            FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger("CommandApi");
            logger.LogInformation("C# HTTP trigger function processed a request.");

            var content = await request.ReadAsStringAsync().ConfigureAwait(false);
            if (string.IsNullOrEmpty(content))
            {
                var errorResponse = request.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync("Missing body").ConfigureAwait(false);
                return errorResponse;
            }

            var response = request.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            var connectionString = Environment.GetEnvironmentVariable("MARKET_DATA_QUEUE_CONNECTION_STRING");
            var topicName = Environment.GetEnvironmentVariable("MARKET_DATA_QUEUE_TOPIC_NAME");

            // create a Service Bus client
            await using (ServiceBusClient client = new ServiceBusClient(connectionString))
            {
                // create a sender for the topic
                var sender = client.CreateSender(topicName);
                var bytes = Encoding.UTF8.GetBytes(content);
                await sender.SendMessageAsync(new ServiceBusMessage(bytes)).ConfigureAwait(false);
                Console.WriteLine($"Sent a single message to the topic: {topicName}");
            }

            return response;
        }
    }
}
