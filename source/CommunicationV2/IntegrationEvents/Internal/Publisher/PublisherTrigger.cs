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

using Energinet.DataHub.Core.App.WebApp.Hosting;
using Energinet.DataHub.Core.Messaging.Communication.Publisher;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.Core.Messaging.Communication.Internal.Publisher;

/// <summary>
/// The sender runs as a background service
/// </summary>
internal sealed class PublisherTrigger : RepeatingTrigger<IPublisher>
{
    public PublisherTrigger(
        IOptions<PublisherWorkerOptions> options,
        IServiceProvider serviceProvider,
        ILogger<PublisherTrigger> logger)
        : base(serviceProvider, logger, TimeSpan.FromMilliseconds(options.Value.HostedServiceExecutionDelayMs))
    {
    }

    protected override async Task ExecuteAsync(
        IPublisher publisher,
        CancellationToken cancellationToken,
        Action isAliveCallback)
    {
        await publisher.PublishAsync(cancellationToken).ConfigureAwait(false);
    }
}
