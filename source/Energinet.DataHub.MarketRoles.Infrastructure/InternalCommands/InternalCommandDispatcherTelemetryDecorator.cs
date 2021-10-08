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
using System.Threading.Tasks;
using Energinet.DataHub.MarketRoles.Infrastructure.Correlation;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace Energinet.DataHub.MarketRoles.Infrastructure.InternalCommands
{
    public class InternalCommandDispatcherTelemetryDecorator : IInternalCommandDispatcher
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly IInternalCommandDispatcher _decoratee;

        public InternalCommandDispatcherTelemetryDecorator(
            TelemetryClient telemetryClient,
            IInternalCommandDispatcher decoratee)
        {
            _telemetryClient = telemetryClient;
            _decoratee = decoratee;
        }

        public async Task<DispatchResult> DispatchAsync(QueuedInternalCommand queuedInternalCommand)
        {
            if (queuedInternalCommand == null) throw new ArgumentNullException(nameof(queuedInternalCommand));

            var traceContext = TraceContext.Parse(queuedInternalCommand.CorrelationId);
            if (!traceContext.IsValid)
            {
                return await _decoratee.DispatchAsync(queuedInternalCommand).ConfigureAwait(false);
            }

            var operation = _telemetryClient.StartOperation<DependencyTelemetry>("InternalCommand", traceContext.TraceId, traceContext.ParentId);
            operation.Telemetry.Type = "Function";
            try
            {
                operation.Telemetry.Success = true;

                return await _decoratee.DispatchAsync(queuedInternalCommand).ConfigureAwait(false);
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
        }
    }
}
