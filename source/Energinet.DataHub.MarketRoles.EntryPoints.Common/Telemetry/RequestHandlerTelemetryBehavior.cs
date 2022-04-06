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
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketRoles.EntryPoints.Common.Telemetry
{
    public class RequestHandlerTelemetryBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull, IRequest<TResponse>
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly ILogger _logger;

        public RequestHandlerTelemetryBehavior(
            TelemetryClient telemetryClient,
            ILogger logger)
        {
            _telemetryClient = telemetryClient;
            _logger = logger;
        }

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            if (next == null) throw new ArgumentNullException(nameof(next));

            var operationName = request.GetType().Name;
            _logger.LogInformation("Handle {Request}", operationName);
            var operation = _telemetryClient.StartOperation<DependencyTelemetry>(operationName);
            operation.Telemetry.Type = "Handler";

            TResponse result;
            try
            {
                result = await next().ConfigureAwait(false);
                operation.Telemetry.Success = true;
            }
            catch (Exception exception)
            {
                operation.Telemetry.Success = false;
                _telemetryClient.TrackException(exception);
                throw;
            }
            finally
            {
                _telemetryClient.StopOperation(operation);
            }

            return result;
        }
    }
}
