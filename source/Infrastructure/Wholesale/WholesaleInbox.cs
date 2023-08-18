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
using Application.Wholesale;
using Domain.Transactions.AggregatedMeasureData;
using Infrastructure.Configuration.MessageBus;
using Infrastructure.Transactions.AggregatedMeasureData;

namespace Infrastructure.Wholesale;

public class WholesaleInbox : IWholesaleInbox
{
    private readonly IServiceBusSenderAdapter _senderCreator;

    public WholesaleInbox(
        IServiceBusSenderFactory serviceBusSenderFactory,
        WholesaleServiceBusClientConfiguration wholeSaleServiceBusClientConfiguration)
    {
        if (serviceBusSenderFactory == null) throw new ArgumentNullException(nameof(serviceBusSenderFactory));
        if (wholeSaleServiceBusClientConfiguration == null) throw new ArgumentNullException(nameof(wholeSaleServiceBusClientConfiguration));

        _senderCreator = serviceBusSenderFactory.GetSender(wholeSaleServiceBusClientConfiguration.QueueName);
    }

    public async Task SendAsync(
        AggregatedMeasureDataProcess request,
        CancellationToken cancellationToken)
    {
        // We transform the request from our side to a response from wholesale,
        // That is, we mimic the response from whole defined in the protobuf contract
        // AggregatedMeasuredDataAcceptedResponse.proto and send it to our own inbox.
        // Such that we can continue implementing peek of aggregated measured data.
        await _senderCreator.SendAsync(
            AggregatedMeasureDataResponseFactory.CreateServiceBusMessage(request),
            cancellationToken).ConfigureAwait(false);
    }
}
