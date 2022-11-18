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
using Messaging.Infrastructure.Configuration.DataAccess;

namespace Messaging.Infrastructure.Configuration.Processing;

public class AssignOutgoingMessagesToBundlesBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
{
    private readonly B2BContext _b2BContext;

    public AssignOutgoingMessagesToBundlesBehaviour(B2BContext b2BContext)
    {
        _b2BContext = b2BContext;
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
        }

        return result;
    }
}
