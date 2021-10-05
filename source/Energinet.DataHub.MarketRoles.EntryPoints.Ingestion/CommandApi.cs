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
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading.Tasks;
using Energinet.DataHub.MarketRoles.Application.Common;
using Energinet.DataHub.MarketRoles.Application.Common.Transport;
using Energinet.DataHub.MarketRoles.Infrastructure.Correlation;
using Energinet.DataHub.MarketRoles.Infrastructure.EDI.XmlConverter;
using Energinet.DataHub.MarketRoles.Infrastructure.Transport;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketRoles.EntryPoints.Ingestion
{
    public class CommandApi
    {
        private readonly ILogger _logger;
        private readonly MessageDispatcher _messageDispatcher;
        private readonly ICorrelationContext _correlationContext;
        private readonly IXmlDeserializer _xmlDeserializer;

        public CommandApi(
            ILogger logger,
            MessageDispatcher messageDispatcher,
            ICorrelationContext correlationContext,
            IXmlDeserializer xmlDeserializer)
        {
            _logger = logger;
            _messageDispatcher = messageDispatcher;
            _correlationContext = correlationContext;
            _xmlDeserializer = xmlDeserializer;
        }

        [Function("CommandApi")]
        public async Task<HttpResponseData> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")]
            [NotNull] HttpRequestData request)
        {
            _logger.LogInformation("Received CommandApi request");

            IEnumerable<IBusinessRequest> commands;

            try
            {
                commands = await _xmlDeserializer.DeserializeAsync(request.Body).ConfigureAwait(false);
            }
#pragma warning disable CA1031 // We need to return BadRequest in case of failure
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unable to deserialize request");
                return request.CreateResponse(HttpStatusCode.BadRequest);
            }

            var response = request.CreateResponse(HttpStatusCode.OK);

            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            await response.WriteStringAsync("Correlation id: " + _correlationContext.Id)
                .ConfigureAwait(false);

            foreach (var command in commands)
            {
                await _messageDispatcher.DispatchAsync((IOutboundMessage)command).ConfigureAwait(false);
            }

            return response;
        }
    }
}
