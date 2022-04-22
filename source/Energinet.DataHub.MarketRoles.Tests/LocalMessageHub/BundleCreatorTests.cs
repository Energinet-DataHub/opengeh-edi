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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using Energinet.DataHub.MarketRoles.EntryPoints.Common.MediatR;
using Energinet.DataHub.MarketRoles.Infrastructure.DataAccess.MessageHub.Bundling;
using Energinet.DataHub.MarketRoles.Infrastructure.EDI;
using Energinet.DataHub.MarketRoles.Infrastructure.EDI.Acknowledgements;
using Energinet.DataHub.MarketRoles.Infrastructure.LocalMessageHub;
using Energinet.DataHub.MarketRoles.Infrastructure.Serialization;
using Energinet.DataHub.MarketRoles.Messaging.Bundling;
using Energinet.DataHub.MarketRoles.Messaging.Bundling.Confirm;
using FluentAssertions;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using SimpleInjector;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketRoles.Tests.LocalMessageHub
{
    [UnitTest]
    public class BundleCreatorTests
    {
        [Fact]
        public async Task Bundles_should_be_created_for_multiple_documents()
        {
            await using var container = new Container();

            container.UseMediatR()
                .WithPipeline()
                .WithRequestHandlers(
                    typeof(ConfirmMessageBundleHandler));

            container.Register<IJsonSerializer, JsonSerializer>();
            container.Register<IBundleCreator, BundleCreator>();
            container.Register<IDocumentSerializer<ConfirmMessage>, ConfirmMessageXmlSerializer>();
            container.Register(() => new TelemetryClient(new TelemetryConfiguration()));
            container.Register<ILogger>(() => NullLogger.Instance, Lifestyle.Singleton);
            var sut = container.GetInstance<IBundleCreator>();
            var confirmMessages = CreateConfirmMessages().ToList();
            var messageHubMessages = CreateMessages(confirmMessages);

            var bundle = await sut.CreateBundleAsync(messageHubMessages).ConfigureAwait(false);

            bundle.Should().NotBeNull();
            bundle.Should().ContainAll(confirmMessages.Select(message => message.MarketActivityRecord.Id.ToString()));
        }

        private static List<MessageHubMessage> CreateMessages(IEnumerable<ConfirmMessage> createConfirmMessages)
        {
            var jsonSerializer = new JsonSerializer();

            var officeMessages = createConfirmMessages
                .Select(message => new
                {
                    Content = jsonSerializer.Serialize(message),
                    GsrnNumber = message.MarketActivityRecord.MarketEvaluationPoint,
                })
                .Select(message => new MessageHubMessage(message.Content, "correlation", DocumentType.ConfirmMoveIn, "recipient", SystemClock.Instance.GetCurrentInstant(), message.GsrnNumber))
                .ToList();
            return officeMessages;
        }

        private static IEnumerable<ConfirmMessage> CreateConfirmMessages()
        {
            return new Fixture().Create<List<ConfirmMessage>>()
                .Select(message => message with { DocumentName = "Foo" });
        }
    }
}
