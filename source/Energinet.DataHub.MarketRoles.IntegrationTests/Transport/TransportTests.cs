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

using System.Threading.Tasks;
using Energinet.DataHub.MarketRoles.Contracts;
using Energinet.DataHub.MarketRoles.EntryPoints.Common.SimpleInjector;
using Energinet.DataHub.MarketRoles.Infrastructure.Transport;
using Energinet.DataHub.MarketRoles.Infrastructure.Transport.Protobuf.Integration;
using Energinet.DataHub.MarketRoles.IntegrationTests.Transport.TestImplementations;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SimpleInjector;
using SimpleInjector.Lifestyles;
using Xunit;

namespace Energinet.DataHub.MarketRoles.IntegrationTests.Transport
{
    public class TransportTests
    {
        [Fact]
        public async Task Send_and_receive_must_result_in_same_transmitted_values()
        {
            var container = new Container();
            container.Options.DefaultScopedLifestyle = new AsyncScopedLifestyle();

            // Send Registrations
            container.Register<InProcessChannel>(Lifestyle.Singleton);
            container.Register<Dispatcher>(Lifestyle.Transient);
            container.SendProtobuf<TestEnvelope>();

            // Receive Registrations
            container.ReceiveProtobuf<TestEnvelope>(
                config => config
                    .FromOneOf(envelope => envelope.TestMessagesCase)
                    .WithParser(() => TestEnvelope.Parser));

            // Verify configuration
            container.Verify();

            using (var scope = AsyncScopedLifestyle.BeginScope(container))
            {
                // Send
                var messageDispatcher = container.GetInstance<Dispatcher>();
                var outboundMessage = new TransportTestRecord("test");
                await messageDispatcher.DispatchAsync(outboundMessage).ConfigureAwait(false);
                var channel = container.GetInstance<InProcessChannel>();

                // The wire
                var bytes = channel.GetWrittenBytes();

                // Receive
                var messageExtractor = container.GetInstance<MessageExtractor>();
                var message = await messageExtractor.ExtractAsync(bytes).ConfigureAwait(false);

                message.Should().BeOfType<TransportTestRecord>();
            }
        }
    }
}
