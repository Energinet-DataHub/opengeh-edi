﻿// Copyright 2020 Energinet DataHub A/S
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
using Energinet.DataHub.EDI.Process.Application.Transactions.WholesaleServices.Commands;
using Energinet.DataHub.EDI.Process.Infrastructure.InternalCommands;
using MediatR;

namespace Energinet.DataHub.EDI.Process.Application.Transactions.WholesaleServices.Notifications.Handlers;

public sealed class
    WhenARejectedWholesaleServicesRequestIsAvailable : INotificationHandler<WholesaleServicesRequestWasRejected>
{
    private readonly CommandSchedulerFacade _commandSchedulerFacade;

    public WhenARejectedWholesaleServicesRequestIsAvailable(CommandSchedulerFacade commandSchedulerFacade)
    {
        _commandSchedulerFacade = commandSchedulerFacade;
    }

    public Task Handle(WholesaleServicesRequestWasRejected notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        return _commandSchedulerFacade.EnqueueAsync(
            new RejectedWholesaleServices(notification.EventId, notification.ReferenceId, notification.RejectReasons));
    }
}
