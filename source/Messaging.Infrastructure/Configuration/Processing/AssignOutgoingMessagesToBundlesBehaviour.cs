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
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using MediatR;
using Messaging.Application.Configuration.Commands.Commands;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Domain.OutgoingMessages;
using Messaging.Infrastructure.Configuration.DataAccess;
using Microsoft.EntityFrameworkCore.Storage;

namespace Messaging.Infrastructure.Configuration.Processing;

public class AssignOutgoingMessagesToBundlesBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
{
    private readonly B2BContext _b2BContext;
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public AssignOutgoingMessagesToBundlesBehaviour(B2BContext b2BContext, IDbConnectionFactory dbConnectionFactory)
    {
        _b2BContext = b2BContext;
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
    {
        ArgumentNullException.ThrowIfNull(next);
        var result = await next().ConfigureAwait(false);

        var outgoingMessages = _b2BContext
            .ChangeTracker
            .Entries<OutgoingMessage>()
            .Select(entity => entity.Entity).ToList();

        foreach (var message in outgoingMessages)
        {
            message.SetBundleId(Guid.NewGuid());
            await StoreToActorMessageQueueAsync(message).ConfigureAwait(false);
        }

        return result;
    }

    private async Task StoreToActorMessageQueueAsync(OutgoingMessage message)
    {
        var sql = @$"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ActorMessageQueue_{message.ReceiverId.Value}' and xtype='U')
        CREATE TABLE [B2B].ActorMessageQueue_{message.ReceiverId.Value}(
            [RecordId]                            [int] IDENTITY (1,1) NOT NULL,
        [Id]                              [uniqueIdentifier] NOT NULL,
        [DocumentType]                    [VARCHAR](255)     NOT NULL,
        [ReceiverId]                      [VARCHAR](255)     NOT NULL,
        [ReceiverRole]                    [VARCHAR](50)      NOT NULL,
        [SenderId]                        [VARCHAR](255)     NOT NULL,
        [SenderRole]                      [VARCHAR](50)      NOT NULL,
        [ProcessType]                     [VARCHAR](50)      NOT NULL,
            CONSTRAINT [PK_ActorMessageQueue_{message.ReceiverId.Value}_Id] PRIMARY KEY NONCLUSTERED
                (
            [Id] ASC
            ) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
            ) ON [PRIMARY];
        INSERT INTO [B2B].[ActorMessageQueue_{message.ReceiverId.Value}] VALUES (@Id, @DocumentType, @ReceiverId, @ReceiverRole, @SenderId, @SenderRole, @ProcessType)";

        await _dbConnectionFactory.GetOpenConnection()
            .ExecuteAsync(
                sql,
                new
                {
                    Id = Guid.NewGuid(),
                    DocumentType = message.DocumentType.Name,
                    ReceiverId = message.ReceiverId.Value,
                    ReceiverRole = message.ReceiverRole.Name,
                    SenderId = message.SenderId.Value,
                    SenderRole = message.SenderRole.Name,
                    message.ProcessType,
                },
                _b2BContext.Database.CurrentTransaction?.GetDbTransaction())
            .ConfigureAwait(false);
    }
}
