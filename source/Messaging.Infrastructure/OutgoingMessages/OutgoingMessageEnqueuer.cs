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
using Dapper;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Domain.OutgoingMessages.Peek;

namespace Messaging.Infrastructure.OutgoingMessages;

public class OutgoingMessageEnqueuer
{
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly IUnitOfWork _unitOfWork;

    public OutgoingMessageEnqueuer(IDbConnectionFactory dbConnectionFactory, IUnitOfWork unitOfWork)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _unitOfWork = unitOfWork;
    }

    public Task EnqueueAsync(EnqueuedMessage message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        var sql = @$"INSERT INTO [B2B].[EnqueuedMessages] VALUES (@Id, @MessageType, @MessageCategory, @ReceiverId, @ReceiverRole, @SenderId, @SenderRole, @ProcessType, @MessageRecord)";

        return _dbConnectionFactory.GetOpenConnection()
            .ExecuteAsync(
                sql,
                new
                {
                    Id = message.Id,
                    MessageType = message.MessageType,
                    MessageCategory = message.Category,
                    ReceiverId = message.ReceiverId,
                    ReceiverRole = message.ReceiverRole,
                    SenderId = message.SenderId,
                    SenderRole = message.SenderRole,
                    ProcessType = message.ProcessType,
                    MessageRecord = message.MessageRecord,
                },
                _unitOfWork.CurrentTransaction);
    }
}
