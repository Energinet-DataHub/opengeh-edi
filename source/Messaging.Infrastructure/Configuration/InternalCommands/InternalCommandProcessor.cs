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
using Dapper;
using Messaging.Application.Configuration.Commands.Commands;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Infrastructure.Configuration.Serialization;
using Microsoft.Extensions.Logging;
using Polly;

namespace Messaging.Infrastructure.Configuration.InternalCommands
{
    public class InternalCommandProcessor
    {
        private readonly InternalCommandMapper _mapper;
        private readonly InternalCommandAccessor _internalCommandAccessor;
        private readonly ISerializer _serializer;
        private readonly CommandExecutor _commandExecutor;
        private readonly ILogger<InternalCommandProcessor> _logger;
        private readonly IEdiDatabaseConnection _connection;

        public InternalCommandProcessor(
            InternalCommandMapper mapper,
            InternalCommandAccessor internalCommandAccessor,
            ISerializer serializer,
            CommandExecutor commandExecutor,
            ILogger<InternalCommandProcessor> logger,
            IEdiDatabaseConnection connection)
        {
            _mapper = mapper;
            _internalCommandAccessor = internalCommandAccessor ?? throw new ArgumentNullException(nameof(internalCommandAccessor));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _commandExecutor = commandExecutor ?? throw new ArgumentNullException(nameof(commandExecutor));
            _logger = logger;
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        public async Task ProcessPendingAsync()
        {
            var pendingCommands = await _internalCommandAccessor.GetPendingAsync().ConfigureAwait(false);

            var executionPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(new[]
                {
                    TimeSpan.FromSeconds(2),
                });

            foreach (var queuedCommand in pendingCommands)
            {
                var result = await executionPolicy.ExecuteAndCaptureAsync(() =>
                    ExecuteCommandAsync(queuedCommand)).ConfigureAwait(false);

                if (result.Outcome == OutcomeType.Failure)
                {
                    var exception = result.FinalException.ToString();
                    await MarkAsFailedAsync(queuedCommand, exception).ConfigureAwait(false);
                    _logger?.Log(LogLevel.Error, result.FinalException, $"Failed to process internal command {queuedCommand.Id}");
                }
                else
                {
                    await MarkAsProcessedAsync(queuedCommand).ConfigureAwait(false);
                }
            }
        }

        private Task ExecuteCommandAsync(QueuedInternalCommand queuedInternalCommand)
        {
            var commandMetaData = _mapper.GetByName(queuedInternalCommand.Type);
            var command = (InternalCommand)_serializer.Deserialize(queuedInternalCommand.Data, commandMetaData.CommandType);
            return _commandExecutor.ExecuteAsync(command, CancellationToken.None);
        }

        private async Task MarkAsFailedAsync(QueuedInternalCommand queuedCommand, string exception)
        {
            using var connection = await _connection.GetConnectionAndOpenAsync().ConfigureAwait(false);
            await connection.ExecuteScalarAsync(
                "UPDATE [b2b].[QueuedInternalCommands] " +
                "SET ProcessedDate = @NowDate, " +
                "ErrorMessage = @Error " +
                "WHERE [Id] = @Id",
                new
                {
                    NowDate = DateTime.UtcNow,
                    Error = exception,
                    queuedCommand.Id,
                }).ConfigureAwait(false);
        }

        private async Task MarkAsProcessedAsync(QueuedInternalCommand queuedCommand)
        {
            using var connection = await _connection.GetConnectionAndOpenAsync().ConfigureAwait(false);
            await connection.ExecuteScalarAsync(
                "UPDATE [b2b].[QueuedInternalCommands] " +
                "SET ProcessedDate = @NowDate " +
                "WHERE [Id] = @Id",
                new
                {
                    NowDate = DateTime.UtcNow,
                    queuedCommand.Id,
                }).ConfigureAwait(false);
        }
    }
}
