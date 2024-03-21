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
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.MessageBus;
using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Process.Domain.Transactions.WholesaleServices;
using Energinet.DataHub.EDI.Process.Domain.Wholesale;
using Energinet.DataHub.EDI.Process.Infrastructure.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Process.Infrastructure.Transactions.WholesaleServices;
using Microsoft.Extensions.Options;
using ServiceBusClientOptions = Energinet.DataHub.EDI.Process.Infrastructure.Configuration.Options.ServiceBusClientOptions;

namespace Energinet.DataHub.EDI.Process.Infrastructure.Wholesale;

public class WholesaleInbox : IWholesaleInbox
{
    private readonly IServiceBusSenderAdapter _senderCreator;

    public WholesaleInbox(
        IServiceBusSenderFactory serviceBusSenderFactory,
        IOptions<ServiceBusClientOptions> options)
    {
        ArgumentNullException.ThrowIfNull(serviceBusSenderFactory);
        ArgumentNullException.ThrowIfNull(options);

        _senderCreator = serviceBusSenderFactory.GetSender(options.Value.WHOLESALE_INBOX_MESSAGE_QUEUE_NAME);
    }

    public async Task SendProcessAsync(
        AggregatedMeasureDataProcess aggregatedMeasureDataProcess,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(aggregatedMeasureDataProcess);
        var serviceBusMessage = AggregatedMeasureDataRequestFactory.CreateServiceBusMessage(aggregatedMeasureDataProcess);
        await _senderCreator.SendAsync(serviceBusMessage, cancellationToken).ConfigureAwait(false);
    }

    public async Task SendProcessAsync(
        WholesaleServicesProcess wholesaleServicesProcess,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(wholesaleServicesProcess);
        var serviceBusMessage = ServiceBusMessageFactory.CreateServiceBusMessage(wholesaleServicesProcess);
        await _senderCreator.SendAsync(serviceBusMessage, cancellationToken).ConfigureAwait(false);
    }
}
