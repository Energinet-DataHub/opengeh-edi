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

using Azure.Messaging.ServiceBus;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FeatureFlag;
using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Process.Domain.Transactions.WholesaleServices;
using Energinet.DataHub.EDI.Process.Domain.Wholesale;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.Options;
using Energinet.DataHub.EDI.Process.Infrastructure.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Process.Infrastructure.Transactions.WholesaleServices;
using Energinet.DataHub.Wholesale.Edi;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.EDI.Process.Infrastructure.Wholesale;

public class WholesaleInboxClient : IWholesaleInboxClient
{
    private readonly IFeatureFlagManager _featureFlagManager;
    private readonly IWholesaleServicesRequestHandler _wholesaleServicesRequestHandler;
    private readonly IAggregatedTimeSeriesRequestHandler _aggregatedTimeSeriesRequestHandler;
    private readonly ServiceBusSender _sender;

    public WholesaleInboxClient(
        IOptions<WholesaleInboxQueueOptions> options,
        IAzureClientFactory<ServiceBusSender> senderFactory,
        IFeatureFlagManager featureFlagManager,
        IWholesaleServicesRequestHandler wholesaleServicesRequestHandler,
        IAggregatedTimeSeriesRequestHandler aggregatedTimeSeriesRequestHandler)
    {
        _featureFlagManager = featureFlagManager;
        _wholesaleServicesRequestHandler = wholesaleServicesRequestHandler;
        _aggregatedTimeSeriesRequestHandler = aggregatedTimeSeriesRequestHandler;
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(senderFactory);

        _sender = senderFactory.CreateClient(options.Value.QueueName);
    }

    public async Task SendProcessAsync(
        AggregatedMeasureDataProcess aggregatedMeasureDataProcess,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(aggregatedMeasureDataProcess);

        var serviceBusMessage = AggregatedMeasureDataRequestFactory.CreateServiceBusMessage(aggregatedMeasureDataProcess);

        if (await _featureFlagManager.RequestStaysInEdiAsync().ConfigureAwait(false))
        {
            await _aggregatedTimeSeriesRequestHandler.ProcessAsync(serviceBusMessage.Body, aggregatedMeasureDataProcess.ProcessId.Id.ToString(), cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await _sender.SendMessageAsync(serviceBusMessage, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task SendProcessAsync(
        WholesaleServicesProcess wholesaleServicesProcess,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(wholesaleServicesProcess);
        var serviceBusMessage = WholesaleServicesRequestFactory.CreateServiceBusMessage(wholesaleServicesProcess);
        if (await _featureFlagManager.RequestStaysInEdiAsync().ConfigureAwait(false))
        {
            await _wholesaleServicesRequestHandler.ProcessAsync(serviceBusMessage.Body, wholesaleServicesProcess.ProcessId.Id.ToString(), cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await _sender.SendMessageAsync(serviceBusMessage, cancellationToken).ConfigureAwait(false);
        }
    }
}
