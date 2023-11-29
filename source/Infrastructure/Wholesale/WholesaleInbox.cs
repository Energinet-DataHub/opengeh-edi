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
using Azure.Messaging.ServiceBus;
using Energinet.DataHub.EDI.Application.Wholesale;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.MessageBus;
using Microsoft.Extensions.Options;
using ServiceBusClientOptions = Energinet.DataHub.EDI.Process.Infrastructure.Configuration.Options.ServiceBusClientOptions;

namespace Energinet.DataHub.EDI.Infrastructure.Wholesale;

public class WholesaleInbox : IWholesaleInbox
{
    private readonly IServiceBusSenderAdapter _senderCreator;

    public WholesaleInbox(
        IServiceBusSenderFactory serviceBusSenderFactory,
        IOptions<ServiceBusClientOptions> options)
    {
        if (serviceBusSenderFactory == null) throw new ArgumentNullException(nameof(serviceBusSenderFactory));
        if (options == null) throw new ArgumentNullException(nameof(options));

        _senderCreator = serviceBusSenderFactory.GetSender(options.Value.INCOMING_MESSAGES_QUEUE_NAME);
    }

    public async Task SendAsync(
        ServiceBusMessage request,
        CancellationToken cancellationToken)
    {
        await _senderCreator.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
