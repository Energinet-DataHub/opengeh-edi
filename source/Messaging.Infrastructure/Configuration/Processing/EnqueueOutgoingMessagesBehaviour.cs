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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Messaging.Application.Configuration.Commands.Commands;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.OutgoingMessages.Peek;
using Messaging.Infrastructure.Configuration.DataAccess;
using Messaging.Infrastructure.Configuration.FeatureFlag;
using Messaging.Infrastructure.OutgoingMessages;
using Microsoft.EntityFrameworkCore;

namespace Messaging.Infrastructure.Configuration.Processing;

public class EnqueueOutgoingMessagesBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
{
    private readonly B2BContext _b2BContext;
    private readonly OutgoingMessageEnqueuer _outgoingMessageEnqueuer;
    private readonly IFeatureFlagProvider _featureFlagProvider;

    public EnqueueOutgoingMessagesBehaviour(B2BContext b2BContext, OutgoingMessageEnqueuer outgoingMessageEnqueuer, IFeatureFlagProvider featureFlagProvider)
    {
        _b2BContext = b2BContext;
        _outgoingMessageEnqueuer = outgoingMessageEnqueuer;
        _featureFlagProvider = featureFlagProvider;
    }

    public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
    {
        ArgumentNullException.ThrowIfNull(next);
        var result = await next().ConfigureAwait(false);

        var outgoingMessages = _b2BContext
            .ChangeTracker
            .Entries<OutgoingMessage>()
            .Where(entity => entity.State == EntityState.Added)
            .Select(entity => entity.Entity).ToList();

        foreach (var message in outgoingMessages)
        {
            await _outgoingMessageEnqueuer.EnqueueAsync(
                new EnqueuedMessage(
                    message.Id,
                    message.ReceiverId.Value,
                    message.ReceiverRole.Name,
                    message.SenderId.Value,
                    message.SenderRole.Name,
                    message.MessageType.Name,
                    message.MessageType.Category.Name,
                    message.ProcessType,
                    message.MessageRecord)).ConfigureAwait(false);
        }

        return result;
    }
}
