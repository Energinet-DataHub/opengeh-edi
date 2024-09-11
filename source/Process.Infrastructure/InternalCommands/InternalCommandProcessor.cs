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

using System.Diagnostics;
using Dapper;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.Process.Domain.Commands;
using Microsoft.Extensions.Logging;
using NodaTime;
using Polly;

namespace Energinet.DataHub.EDI.Process.Infrastructure.InternalCommands;

public class InternalCommandProcessor
{
    private readonly InternalCommandMapper _mapper;
    private readonly InternalCommandAccessor _internalCommandAccessor;
    private readonly ISerializer _serializer;
    private readonly CommandExecutor _commandExecutor;
    private readonly ILogger<InternalCommandProcessor> _logger;
    private readonly IDatabaseConnectionFactory _connectionFactory;
    private readonly IClock _clock;

    public InternalCommandProcessor(
        InternalCommandMapper mapper,
        InternalCommandAccessor internalCommandAccessor,
        ISerializer serializer,
        CommandExecutor commandExecutor,
        ILogger<InternalCommandProcessor> logger,
        IDatabaseConnectionFactory connectionFactory,
        IClock clock)
    {
        _mapper = mapper;
        _internalCommandAccessor = internalCommandAccessor ?? throw new ArgumentNullException(nameof(internalCommandAccessor));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _commandExecutor = commandExecutor ?? throw new ArgumentNullException(nameof(commandExecutor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _clock = clock;
    }

    public async Task ProcessPendingAsync(CancellationToken cancellationToken)
    {
        var stopwatch = new Stopwatch();
        var innerStopWatch = new Stopwatch();
        stopwatch.Start();
        var pendingCommands = await _internalCommandAccessor
            .GetPendingAsync().ConfigureAwait(false);

        var executionPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(new[]
            {
                TimeSpan.FromSeconds(2),
            });

        foreach (var queuedCommand in pendingCommands)
        {
            innerStopWatch.Start();
            var result = await executionPolicy.ExecuteAndCaptureAsync(() =>
                ExecuteCommandAsync(queuedCommand)).ConfigureAwait(false);

            if (result.Outcome == OutcomeType.Failure)
            {
                var exception = result.FinalException.ToString();
                await MarkAsFailedAsync(queuedCommand, exception, cancellationToken).ConfigureAwait(false);
                _logger?.Log(LogLevel.Error, result.FinalException, "Failed to process internal command. Id: {CommandId}, Type: {CommandType}", queuedCommand.Id, queuedCommand.Type);
            }
            else
            {
                await MarkAsProcessedAsync(queuedCommand, cancellationToken).ConfigureAwait(false);
            }

            innerStopWatch.Stop();
            _logger?.Log(
                innerStopWatch.ElapsedMilliseconds < 500m
                    ? LogLevel.Debug
                    : LogLevel.Warning,
                "{Command} executed in {ElapsedMilliseconds} ms",
                queuedCommand.Type,
                innerStopWatch.ElapsedMilliseconds);

            innerStopWatch.Reset();
        }

        stopwatch.Stop();
        _logger?.Log(LogLevel.Information, "Internal Command Processor executed in {ElapsedMilliseconds} ms", stopwatch.ElapsedMilliseconds);
    }

    private Task ExecuteCommandAsync(QueuedInternalCommand queuedInternalCommand)
    {
        var commandMetaData = _mapper.GetByName(queuedInternalCommand.Type);
        var command = (InternalCommand)_serializer.Deserialize(queuedInternalCommand.Data, commandMetaData.CommandType);
        return _commandExecutor.ExecuteAsync(command, CancellationToken.None);
    }

    private async Task MarkAsFailedAsync(QueuedInternalCommand queuedCommand, string exception, CancellationToken cancellationToken)
    {
        using var connection = await _connectionFactory.GetConnectionAndOpenAsync(cancellationToken).ConfigureAwait(false);
        await connection.ExecuteScalarAsync(
            "UPDATE [dbo].[QueuedInternalCommands] " +
            "SET ProcessedDate = @NowDate, " +
            "ErrorMessage = @Error " +
            "WHERE [Id] = @Id",
            new
            {
                NowDate = _clock.GetCurrentInstant().ToDateTimeUtc(),
                Error = exception,
                queuedCommand.Id,
            }).ConfigureAwait(false);
    }

    private async Task MarkAsProcessedAsync(QueuedInternalCommand queuedCommand, CancellationToken cancellationToken)
    {
        using var connection = await _connectionFactory.GetConnectionAndOpenAsync(cancellationToken).ConfigureAwait(false);
        await connection.ExecuteScalarAsync(
            "UPDATE [dbo].[QueuedInternalCommands] " +
            "SET ProcessedDate = @NowDate " +
            "WHERE [Id] = @Id",
            new
            {
                NowDate = _clock.GetCurrentInstant().ToDateTimeUtc(),
                queuedCommand.Id,
            }).ConfigureAwait(false);
    }
}
