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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.SubsystemTests.Drivers.MessageFactories;

namespace Energinet.DataHub.EDI.SubsystemTests.Drivers;

public class ProcessManagerDriver(
    EdiTopicClient ediTopicClient)
{
    private readonly EdiTopicClient _ediTopicClient = ediTopicClient;

    internal async Task PublishAcceptedRequestBrs026Async(string gridArea, Actor actor)
    {
        var message = EnqueueBrs026MessageFactory.CreateAccept(actor, gridArea);
        await _ediTopicClient.SendAsync(message, CancellationToken.None).ConfigureAwait(false);
    }

    internal async Task PublishRejectedRequestBrs026Async(Actor actor)
    {
        var message = EnqueueBrs026MessageFactory.CreateReject(actor);
        await _ediTopicClient.SendAsync(message, CancellationToken.None).ConfigureAwait(false);
    }

    internal async Task PublishAcceptedRequestBrs028Async(string gridArea, Actor actor)
    {
        var message = EnqueueBrs028MessageFactory.CreateAccept(actor, gridArea);
        await _ediTopicClient.SendAsync(message, CancellationToken.None).ConfigureAwait(false);
    }

    internal async Task PublishRejectedRequestBrs028Async(Actor actor)
    {
        var message = EnqueueBrs028MessageFactory.CreateReject(actor);
        await _ediTopicClient.SendAsync(message, CancellationToken.None).ConfigureAwait(false);
    }
}
