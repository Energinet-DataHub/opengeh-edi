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

using Dapper;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.Process.Domain.Commands;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.Process.Infrastructure.Processing;

public sealed class UnitOfWorkBehaviour<TRequest, TResponse>(
    IUnitOfWork unitOfWork,
    IDatabaseConnectionFactory databaseConnectionFactory,
    ILogger<UnitOfWorkBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IDatabaseConnectionFactory _databaseConnectionFactory = databaseConnectionFactory;
    private readonly ILogger<UnitOfWorkBehaviour<TRequest, TResponse>> _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        var result = await next().ConfigureAwait(false);

        if (await InternalCommandAlreadyMarkedAsProcessedAsync(request, cancellationToken).ConfigureAwait(false))
        {
            var commandId = (request as InternalCommand)?.Id;
            _logger.Log(
                LogLevel.Information,
                "Command (id: {CommandId}, type: {CommandType}) was processed. All changes will be discarded",
                commandId,
                request.GetType());
        }
        else
        {
            await _unitOfWork.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
        }

        return result;
    }

    private async Task<bool> InternalCommandAlreadyMarkedAsProcessedAsync(
        TRequest request,
        CancellationToken cancellationToken)
    {
        if (request is not InternalCommand internalCommand) return false;

        var checkStatement =
            $"SELECT COUNT(1) FROM [dbo].[QueuedInternalCommands] WHERE Id = '{internalCommand.Id}' AND ProcessedDate IS NOT NULL";
        using var connection =
            (SqlConnection)await _databaseConnectionFactory.GetConnectionAndOpenAsync(cancellationToken)
                .ConfigureAwait(false);

        return await connection.ExecuteScalarAsync<bool>(checkStatement).ConfigureAwait(false);
    }
}
